using BlogSite.CORE.Enums;

namespace BlogSite.CORE.Dtos
{
    public class CreatePostDto
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public PostType Type { get; set; }
        public PostStatus Status { get; set; } = PostStatus.Draft;
        public string[]? Tags { get; set; }
        public Guid AuthorId { get; set; }
    }
}
