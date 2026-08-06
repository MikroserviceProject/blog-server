using System.ComponentModel.DataAnnotations;

namespace AuthenticationService.Core.DTOs;

public class AuthorApplicationDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? University { get; set; }
    public string? CvUrl { get; set; }
    public string? AuthorApprovalStatus { get; set; }
    public DateTime? AuthorApplicationDate { get; set; }
    public string? AuthorRejectionReason { get; set; }
    public bool IsEmailConfirmed { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RejectAuthorRequestDto
{
    [StringLength(500, ErrorMessage = "Ret gerekçesi en fazla 500 karakter olabilir.")]
    public string? Reason { get; set; }
}
