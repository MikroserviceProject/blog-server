using System.ComponentModel.DataAnnotations;

namespace BlogSite.CORE.Dtos
{
    public class AdminDeletePostDto
    {
        [Required(ErrorMessage = "Silme gerekçesi zorunludur.")]
        [MinLength(5, ErrorMessage = "Silme gerekçesi en az 5 karakter olmalıdır.")]
        public string Reason { get; set; } = string.Empty;

        public string? AuthorEmail { get; set; }
        public string? AuthorUsername { get; set; }
    }
}
