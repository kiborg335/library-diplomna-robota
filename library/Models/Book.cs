using System.Collections.Generic;

namespace LibraryApp.Models
{
    public class Book
    {
        public int BookId { get; set; }
        public int GutenbergId { get; set; }
        public string Title { get; set; }
        public int? Year { get; set; }
        public string Language { get; set; }
        public int Downloads { get; set; }
        public string Description { get; set; }
        public string CoverUrl { get; set; }
        public string TextStatus { get; set; } = "catalog";
        public string TextPath { get; set; }
        public List<int> AuthorIds { get; set; } = new List<int>();
        public List<int> GenreIds { get; set; } = new List<int>();
        public string FullText { get; set; }
    }

    public class Author
    {
        public int AuthorId { get; set; }
        public string Name { get; set; }
    }

    public class Genre
    {
        public int GenreId { get; set; }
        public string Name { get; set; }
    }

    public class BookListItem
    {
        public int BookId { get; set; }
        public string Title { get; set; }
        public int? Year { get; set; }
        public string Authors { get; set; }
        public string Genres { get; set; }
        public string Language { get; set; }
        public string TextStatus { get; set; }
        public string AddedDate { get; set; }
    }
}