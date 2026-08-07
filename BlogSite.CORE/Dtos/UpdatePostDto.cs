using BlogSite.CORE.Enums;

namespace BlogSite.CORE.Dtos
{
    public class UpdatePostDto
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public PostStatus Status { get; set; }
        public string PhotoUrl { get; set; }
        public PostType Type { get; set; }
    }
}
