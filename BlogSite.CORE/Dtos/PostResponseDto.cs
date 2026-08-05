using BlogSite.CORE.Enums;

namespace BlogSite.CORE.Dtos
{
    public class PostResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public PostType Type { get; set; }
        public PostStatus Status { get; set; }
        public string? PhotoUrl { get; set; }
        public Guid AuthorId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
