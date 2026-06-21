using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using LibraryApp.Models;

namespace LibraryApp.Database
{
    public class LibraryDatabase
    {
        private readonly string _connectionString;
        private readonly string _basePath;

        public LibraryDatabase(string dbPath = null)
        {
            _basePath = AppDomain.CurrentDomain.BaseDirectory;
            if (dbPath == null)
                dbPath = Path.Combine(_basePath, "library.db");
            _connectionString = "Data Source=" + dbPath + ";Version=3;";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                string sql = @"
                    CREATE TABLE IF NOT EXISTS Authors (
                        author_id INTEGER PRIMARY KEY AUTOINCREMENT,
                        name TEXT NOT NULL UNIQUE
                    );
                    CREATE TABLE IF NOT EXISTS Genres (
                        genre_id INTEGER PRIMARY KEY AUTOINCREMENT,
                        name TEXT NOT NULL UNIQUE
                    );
                    CREATE TABLE IF NOT EXISTS Books (
                        book_id INTEGER PRIMARY KEY AUTOINCREMENT,
                        gutenberg_id INTEGER UNIQUE NOT NULL,
                        title TEXT NOT NULL,
                        year INTEGER,
                        language TEXT DEFAULT 'en',
                        downloads INTEGER DEFAULT 0,
                        description TEXT,
                        cover_url TEXT,
                        text_status TEXT DEFAULT 'catalog',
                        text_path TEXT,
                        added_date TEXT DEFAULT (datetime('now','localtime'))
                    );
                    CREATE TABLE IF NOT EXISTS Book_Authors (
                        book_id INTEGER NOT NULL,
                        author_id INTEGER NOT NULL,
                        PRIMARY KEY (book_id, author_id),
                        FOREIGN KEY (book_id) REFERENCES Books(book_id) ON DELETE CASCADE,
                        FOREIGN KEY (author_id) REFERENCES Authors(author_id) ON DELETE CASCADE
                    );
                    CREATE TABLE IF NOT EXISTS Book_Genres (
                        book_id INTEGER NOT NULL,
                        genre_id INTEGER NOT NULL,
                        PRIMARY KEY (book_id, genre_id),
                        FOREIGN KEY (book_id) REFERENCES Books(book_id) ON DELETE CASCADE,
                        FOREIGN KEY (genre_id) REFERENCES Genres(genre_id) ON DELETE CASCADE
                    );
                    PRAGMA foreign_keys = ON;
                ";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public int AddOrGetAuthor(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return -1;
            name = name.Trim();

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                using (var checkCmd = new SQLiteCommand(
                    "SELECT author_id FROM Authors WHERE name = @name", conn))
                {
                    checkCmd.Parameters.AddWithValue("@name", name);
                    var result = checkCmd.ExecuteScalar();
                    if (result != null) return Convert.ToInt32(result);
                }

                using (var insertCmd = new SQLiteCommand(
                    "INSERT INTO Authors (name) VALUES (@name); SELECT last_insert_rowid();", conn))
                {
                    insertCmd.Parameters.AddWithValue("@name", name);
                    return Convert.ToInt32(insertCmd.ExecuteScalar());
                }
            }
        }

        public List<Author> GetAllAuthors()
        {
            var authors = new List<Author>();
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(
                    "SELECT author_id, name FROM Authors ORDER BY name", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        authors.Add(new Author { AuthorId = reader.GetInt32(0), Name = reader.GetString(1) });
                }
            }
            return authors;
        }

        public int AddOrGetGenre(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return -1;
            name = name.Trim();

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                using (var checkCmd = new SQLiteCommand(
                    "SELECT genre_id FROM Genres WHERE name = @name", conn))
                {
                    checkCmd.Parameters.AddWithValue("@name", name);
                    var result = checkCmd.ExecuteScalar();
                    if (result != null) return Convert.ToInt32(result);
                }

                using (var insertCmd = new SQLiteCommand(
                    "INSERT INTO Genres (name) VALUES (@name); SELECT last_insert_rowid();", conn))
                {
                    insertCmd.Parameters.AddWithValue("@name", name);
                    return Convert.ToInt32(insertCmd.ExecuteScalar());
                }
            }
        }

        public List<Genre> GetAllGenres()
        {
            var genres = new List<Genre>();
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(
                    "SELECT genre_id, name FROM Genres ORDER BY name", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        genres.Add(new Genre { GenreId = reader.GetInt32(0), Name = reader.GetString(1) });
                }
            }
            return genres;
        }

        public int AddBook(Book book)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        string insertBook = @"
                            INSERT INTO Books (gutenberg_id, title, year, language, downloads, description, cover_url, text_status, text_path)
                            VALUES (@gid, @title, @year, @lang, @downloads, @desc, @cover, @status, @path);
                            SELECT last_insert_rowid();";

                        int bookId;
                        using (var cmd = new SQLiteCommand(insertBook, conn))
                        {
                            cmd.Parameters.AddWithValue("@gid", book.GutenbergId);
                            cmd.Parameters.AddWithValue("@title", book.Title ?? "");
                            cmd.Parameters.AddWithValue("@year", (object)book.Year ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@lang", book.Language ?? "en");
                            cmd.Parameters.AddWithValue("@downloads", book.Downloads);
                            cmd.Parameters.AddWithValue("@desc", (object)book.Description ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cover", (object)book.CoverUrl ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@status", book.TextStatus ?? "catalog");
                            cmd.Parameters.AddWithValue("@path", (object)book.TextPath ?? DBNull.Value);
                            bookId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        if (book.AuthorIds != null)
                        {
                            foreach (int authorId in book.AuthorIds.Distinct())
                            {
                                using (var linkCmd = new SQLiteCommand(
                                    "INSERT OR IGNORE INTO Book_Authors (book_id, author_id) VALUES (@bid, @aid)", conn))
                                {
                                    linkCmd.Parameters.AddWithValue("@bid", bookId);
                                    linkCmd.Parameters.AddWithValue("@aid", authorId);
                                    linkCmd.ExecuteNonQuery();
                                }
                            }
                        }

                        if (book.GenreIds != null)
                        {
                            foreach (int genreId in book.GenreIds.Distinct())
                            {
                                using (var linkCmd = new SQLiteCommand(
                                    "INSERT OR IGNORE INTO Book_Genres (book_id, genre_id) VALUES (@bid, @gid)", conn))
                                {
                                    linkCmd.Parameters.AddWithValue("@bid", bookId);
                                    linkCmd.Parameters.AddWithValue("@gid", genreId);
                                    linkCmd.ExecuteNonQuery();
                                }
                            }
                        }

                        transaction.Commit();
                        return bookId;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public List<BookListItem> SearchBooks(string keyword = "", string language = "", string genre = "", string status = "")
        {
            var books = new List<BookListItem>();
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                var conditions = new List<string>();
                var parameters = new List<SQLiteParameter>();

                if (!string.IsNullOrEmpty(keyword))
                {
                    conditions.Add("(b.title LIKE @kw OR a.name LIKE @kw)");
                    parameters.Add(new SQLiteParameter("@kw", "%" + keyword + "%"));
                }
                if (!string.IsNullOrEmpty(language))
                {
                    conditions.Add("b.language = @lang");
                    parameters.Add(new SQLiteParameter("@lang", language));
                }
                if (!string.IsNullOrEmpty(genre))
                {
                    conditions.Add("g.name = @genre");
                    parameters.Add(new SQLiteParameter("@genre", genre));
                }
                if (!string.IsNullOrEmpty(status))
                {
                    conditions.Add("b.text_status = @status");
                    parameters.Add(new SQLiteParameter("@status", status));
                }
                else
                {
                    conditions.Add("b.text_status IN ('catalog', 'downloaded')");
                }

                string whereClause = "";
                if (conditions.Count > 0)
                    whereClause = "WHERE " + string.Join(" AND ", conditions);

                string sql = @"
                    SELECT b.book_id, b.title, b.language, b.text_status, b.added_date,
                           GROUP_CONCAT(a.name, ', ') as authors,
                           GROUP_CONCAT(g.name, ', ') as genres
                    FROM Books b
                    LEFT JOIN Book_Authors ba ON b.book_id = ba.book_id
                    LEFT JOIN Authors a ON ba.author_id = a.author_id
                    LEFT JOIN Book_Genres bg ON b.book_id = bg.book_id
                    LEFT JOIN Genres g ON bg.genre_id = g.genre_id
                    " + whereClause + @"
                    GROUP BY b.book_id
                    ORDER BY b.added_date DESC
                    LIMIT 500";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddRange(parameters.ToArray());
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var book = new BookListItem();
                            book.BookId = reader.GetInt32(0);
                            book.Title = reader.GetString(1);
                            book.Language = reader.IsDBNull(2) ? "" : reader.GetString(2);
                            book.TextStatus = reader.IsDBNull(3) ? "catalog" : reader.GetString(3);
                            book.AddedDate = reader.IsDBNull(4) ? "" : reader.GetString(4);
                            book.Authors = reader.IsDBNull(5) ? "" : reader.GetString(5);
                            book.Genres = reader.IsDBNull(6) ? "" : reader.GetString(6);
                            books.Add(book);
                        }
                    }
                }
            }
            return books;
        }

        public Book GetBookById(int bookId)
        {
            Book book = null;
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                string sql = @"
                    SELECT b.book_id, b.gutenberg_id, b.title, b.year, b.language, 
                           b.downloads, b.description, b.cover_url, b.text_status, b.text_path
                    FROM Books b WHERE b.book_id = @id";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", bookId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            book = new Book();
                            book.BookId = reader.GetInt32(0);
                            book.GutenbergId = reader.GetInt32(1);
                            book.Title = reader.GetString(2);
                            book.Year = reader.IsDBNull(3) ? null : (int?)reader.GetInt32(3);
                            book.Language = reader.IsDBNull(4) ? "en" : reader.GetString(4);
                            book.Downloads = reader.IsDBNull(5) ? 0 : reader.GetInt32(5);
                            book.Description = reader.IsDBNull(6) ? null : reader.GetString(6);
                            book.CoverUrl = reader.IsDBNull(7) ? null : reader.GetString(7);
                            book.TextStatus = reader.IsDBNull(8) ? "catalog" : reader.GetString(8);
                            book.TextPath = reader.IsDBNull(9) ? null : reader.GetString(9);
                            book.AuthorIds = new List<int>();
                            book.GenreIds = new List<int>();
                        }
                    }
                }

                if (book == null) return null;

                using (var authorCmd = new SQLiteCommand(@"
                    SELECT a.author_id FROM Authors a 
                    JOIN Book_Authors ba ON a.author_id = ba.author_id 
                    WHERE ba.book_id = @id", conn))
                {
                    authorCmd.Parameters.AddWithValue("@id", bookId);
                    using (var authorReader = authorCmd.ExecuteReader())
                    {
                        while (authorReader.Read())
                            book.AuthorIds.Add(authorReader.GetInt32(0));
                    }
                }

                using (var genreCmd = new SQLiteCommand(@"
                    SELECT g.genre_id FROM Genres g 
                    JOIN Book_Genres bg ON g.genre_id = bg.genre_id 
                    WHERE bg.book_id = @id", conn))
                {
                    genreCmd.Parameters.AddWithValue("@id", bookId);
                    using (var genreReader = genreCmd.ExecuteReader())
                    {
                        while (genreReader.Read())
                            book.GenreIds.Add(genreReader.GetInt32(0));
                    }
                }
            }
            return book;
        }

        public string GetBookText(int bookId)
        {
            string textPath = null;
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var checkCmd = new SQLiteCommand(
                    "SELECT text_path FROM Books WHERE book_id = @id", conn))
                {
                    checkCmd.Parameters.AddWithValue("@id", bookId);
                    var result = checkCmd.ExecuteScalar();
                    if (result != null)
                        textPath = result.ToString();
                }
            }

            if (!string.IsNullOrEmpty(textPath) && File.Exists(textPath))
            {
                string ext = Path.GetExtension(textPath).ToLower();
                if (ext == ".txt")
                {
                    return File.ReadAllText(textPath);
                }
            }

            return null;
        }

        public void SaveBookText(int bookId, string text, int gutenbergId)
        {
            string textDir = Path.Combine(_basePath, "Texts");
            if (!Directory.Exists(textDir))
                Directory.CreateDirectory(textDir);
            string textPath = Path.Combine(textDir, gutenbergId + ".txt");

            File.WriteAllText(textPath, text);

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(
                    "UPDATE Books SET text_status = 'downloaded', text_path = @path WHERE book_id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@path", textPath);
                    cmd.Parameters.AddWithValue("@id", bookId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void DeleteBook(int bookId)
        {
            var book = GetBookById(bookId);
            if (book != null && !string.IsNullOrEmpty(book.TextPath) && File.Exists(book.TextPath))
            {
                try
                {
                    File.Delete(book.TextPath);
                }
                catch { }
            }

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM Books WHERE book_id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", bookId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public int GetBookCount()
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM Books", conn))
                {
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public List<BookListItem> GetMyLibraryBooks()
        {
            var books = new List<BookListItem>();
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                string sql = @"
                    SELECT b.book_id, b.title, b.language, b.text_status, b.added_date,
                           GROUP_CONCAT(a.name, ', ') as authors,
                           GROUP_CONCAT(g.name, ', ') as genres
                    FROM Books b
                    LEFT JOIN Book_Authors ba ON b.book_id = ba.book_id
                    LEFT JOIN Authors a ON ba.author_id = a.author_id
                    LEFT JOIN Book_Genres bg ON b.book_id = bg.book_id
                    LEFT JOIN Genres g ON bg.genre_id = g.genre_id
                    WHERE b.text_status IN ('downloaded', 'custom_pdf', 'custom_docx', 'custom_txt', 'custom')
                    GROUP BY b.book_id
                    ORDER BY b.added_date DESC
                    LIMIT 500";

                using (var cmd = new SQLiteCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var book = new BookListItem();
                        book.BookId = reader.GetInt32(0);
                        book.Title = reader.GetString(1);
                        book.Language = reader.IsDBNull(2) ? "" : reader.GetString(2);
                        book.TextStatus = reader.IsDBNull(3) ? "" : reader.GetString(3);
                        book.AddedDate = reader.IsDBNull(4) ? "" : reader.GetString(4);
                        book.Authors = reader.IsDBNull(5) ? "" : reader.GetString(5);
                        book.Genres = reader.IsDBNull(6) ? "" : reader.GetString(6);
                        books.Add(book);
                    }
                }
            }
            return books;
        }

        public int GetNextCustomId()
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(
                    "SELECT COALESCE(MIN(gutenberg_id), 0) - 1 FROM Books WHERE gutenberg_id < 0", conn))
                {
                    var result = cmd.ExecuteScalar();
                    int id = result != null ? Convert.ToInt32(result) : -1;
                    if (id >= 0) id = -1;
                    return id;
                }
            }
        }
    }
}