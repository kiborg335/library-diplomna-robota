using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using LibraryApp.Database;
using LibraryApp.Models;

namespace LibraryApp.Services
{
    public class GutenbergProgress
    {
        public string Message { get; set; }
        public int Percent { get; set; }
    }

    public class GutenbergImporter
    {
        private readonly LibraryDatabase _db;
        private readonly HttpClient _client;
        private const string CATALOG_URL = "https://www.gutenberg.org/cache/epub/feeds/pg_catalog.csv";

        public GutenbergImporter(LibraryDatabase db)
        {
            _db = db;
            _client = new HttpClient();
            _client.Timeout = TimeSpan.FromMinutes(10);
        }

        public async Task ImportCatalogAsync(IProgress<GutenbergProgress> progress)
        {
            progress.Report(new GutenbergProgress
            {
                Message = "Завантажую каталог...",
                Percent = 0
            });

            string csvContent;
            try
            {
                csvContent = await _client.GetStringAsync(CATALOG_URL);
            }
            catch (Exception ex)
            {
                throw new Exception("Помилка завантаження: " + ex.Message);
            }

            progress.Report(new GutenbergProgress
            {
                Message = "Розбираю CSV...",
                Percent = 5
            });

            var lines = csvContent.Split('\n');

            int total = lines.Length - 1;
            int processed = 0;
            int added = 0;
            int skipped = 0;

            progress.Report(new GutenbergProgress
            {
                Message = "Рядків: " + total + ". Імпортую...",
                Percent = 10
            });

            for (int i = 1; i < lines.Length; i++)
            {
                processed++;

                if (string.IsNullOrWhiteSpace(lines[i]))
                {
                    skipped++;
                    continue;
                }

                try
                {
                    var cols = ParseCsvLine(lines[i]);

                    // Мінімальна перевірка: потрібен ID
                    if (cols.Length < 2)
                    {
                        skipped++;
                        continue;
                    }

                    int textNum;
                    if (!int.TryParse(cols[0].Trim(), out textNum) || textNum <= 0)
                    {
                        skipped++;
                        continue;
                    }

                    // Назва — колонка 3
                    string title = cols.Length > 3 ? cols[3].Trim() : "";
                    if (string.IsNullOrEmpty(title))
                    {
                        // Пробуємо знайти назву в інших колонках
                        for (int t = 4; t < cols.Length; t++)
                        {
                            string val = cols[t].Trim();
                            if (!string.IsNullOrEmpty(val) && val.Length > 3 && !val.StartsWith("http"))
                            {
                                title = val;
                                break;
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(title))
                    {
                        title = "Book #" + textNum;
                    }

                    // Мова — колонка 4
                    string lang = "en";
                    if (cols.Length > 4 && !string.IsNullOrWhiteSpace(cols[4]))
                    {
                        string l = cols[4].Trim();
                        if (l.Length <= 10)
                            lang = l;
                    }

                    // Рік — колонка 2
                    string yearStr = "";
                    if (cols.Length > 2 && !string.IsNullOrWhiteSpace(cols[2]))
                    {
                        int year;
                        if (int.TryParse(cols[2].Trim(), out year) && year > 0 && year < 2100)
                            yearStr = " (" + year + ")";
                    }

                    // Автори — колонка 5
                    string authorStr = "";
                    if (cols.Length > 5 && !string.IsNullOrWhiteSpace(cols[5]))
                    {
                        var authors = cols[5].Split(';');
                        var authorList = new List<string>();
                        foreach (var author in authors)
                        {
                            string a = author.Trim();
                            if (!string.IsNullOrEmpty(a))
                                authorList.Add(a);
                        }
                        if (authorList.Count > 0)
                            authorStr = string.Join(", ", authorList);
                    }

                    // Автори та рік разом
                    string authorAndYear = "";
                    if (!string.IsNullOrEmpty(authorStr) && !string.IsNullOrEmpty(yearStr))
                        authorAndYear = authorStr + yearStr;
                    else if (!string.IsNullOrEmpty(authorStr))
                        authorAndYear = authorStr;
                    else if (!string.IsNullOrEmpty(yearStr))
                        authorAndYear = "Unknown" + yearStr;
                    else
                        authorAndYear = "Unknown";

                    var book = new Book();
                    book.GutenbergId = textNum;
                    book.Title = title;
                    book.Language = lang;
                    book.Downloads = 0;
                    book.TextStatus = "catalog";
                    book.AuthorIds = new List<int>();
                    book.GenreIds = new List<int>();

                    int aid = _db.AddOrGetAuthor(authorAndYear);
                    if (aid > 0) book.AuthorIds.Add(aid);

                    // Теми — усі колонки після 6
                    for (int j = 6; j < cols.Length; j++)
                    {
                        if (!string.IsNullOrWhiteSpace(cols[j]))
                        {
                            foreach (var s in cols[j].Split(';'))
                            {
                                string g = s.Trim();
                                if (!string.IsNullOrEmpty(g) && g.Length < 300)
                                {
                                    int gid = _db.AddOrGetGenre(g);
                                    if (gid > 0) book.GenreIds.Add(gid);
                                }
                            }
                        }
                    }

                    _db.AddBook(book);
                    added++;
                }
                catch
                {
                    skipped++;
                }

                if (processed % 100 == 0)
                {
                    int pct = 15 + (int)((double)processed / total * 80);
                    if (pct > 98) pct = 98;

                    progress.Report(new GutenbergProgress
                    {
                        Message = processed + "/" + total + " | Додано: " + added + " | Пропущено: " + skipped,
                        Percent = pct
                    });
                }
            }

            progress.Report(new GutenbergProgress
            {
                Message = "ГОТОВО! Додано: " + added + " | Пропущено: " + skipped,
                Percent = 100
            });
        }

        private string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            string current = "";

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current);
                    current = "";
                }
                else
                {
                    current += c;
                }
            }
            result.Add(current);

            return result.ToArray();
        }
    }
}