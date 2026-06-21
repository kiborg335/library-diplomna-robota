using System;
using System.Net.Http;
using System.Threading.Tasks;
using LibraryApp.Database;

namespace LibraryApp.Services
{
    public class BookDownloader
    {
        private readonly LibraryDatabase _db;
        private readonly HttpClient _client;

        public BookDownloader(LibraryDatabase db)
        {
            _db = db;
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("User-Agent", "LibraryApp/1.0");
        }

        public async Task<string> DownloadBookTextAsync(int bookId, int gutenbergId)
        {
            string[] possibleUrls = new string[]
            {
                "https://www.gutenberg.org/files/" + gutenbergId + "/" + gutenbergId + "-0.txt",
                "https://www.gutenberg.org/cache/epub/" + gutenbergId + "/pg" + gutenbergId + ".txt",
                "https://www.gutenberg.org/ebooks/" + gutenbergId + ".txt.utf-8"
            };

            foreach (string url in possibleUrls)
            {
                try
                {
                    var response = await _client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        string rawText = await response.Content.ReadAsStringAsync();
                        string cleanText = CleanGutenbergText(rawText);

                        _db.SaveBookText(bookId, cleanText, gutenbergId);

                        return cleanText;
                    }
                }
                catch
                {
                    continue;
                }
            }

            throw new Exception("Не вдалося завантажити текст книги. Перевірте з'єднання.");
        }

        private string CleanGutenbergText(string rawText)
        {
            string startMarker = "*** START OF THE PROJECT GUTENBERG EBOOK";
            string endMarker = "*** END OF THE PROJECT GUTENBERG EBOOK";

            int start = rawText.IndexOf(startMarker, StringComparison.OrdinalIgnoreCase);
            int end = rawText.IndexOf(endMarker, StringComparison.OrdinalIgnoreCase);

            if (start >= 0 && end > start)
            {
                start = rawText.IndexOf('\n', start) + 1;
                return rawText.Substring(start, end - start).Trim();
            }

            return rawText.Trim();
        }
    }
}