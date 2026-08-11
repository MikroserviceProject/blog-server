using BlogSite.CORE.Enums;

namespace BlogSite.CORE.Entities
{
    public class Post
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public PostType Type { get; set; }
        public PostStatus Status { get; set; } = PostStatus.Draft;
        public string? PhotoUrl { get; set; }
        public Guid AuthorId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        // Etiketler özelliği: "Ruby", "Java" vb. için
        public string[] Tags { get; set; } = Array.Empty<string>();
    }
}
