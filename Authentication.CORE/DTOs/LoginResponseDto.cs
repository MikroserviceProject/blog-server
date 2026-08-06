namespace AuthenticationService.Core.DTOs;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresInMinutes { get; set; }
    public UserDto User { get; set; } = null!;
}
