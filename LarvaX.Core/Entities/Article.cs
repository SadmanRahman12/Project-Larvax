namespace LarvaX.Core.Entities
{
    public class Article
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Language { get; set; } = "en"; // 'en' or 'bn'
        public DateTime PublishedDate { get; set; } = DateTime.UtcNow;
        public string? AuthorId { get; set; }
        public ApplicationUser? Author { get; set; }
    }
}
