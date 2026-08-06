using System.Security.Claims;
using AuthenticationService.Core.Entities;

namespace AuthenticationService.Core.Interfaces;

public interface IJwtService
{
    string GenerateToken(User user, out int expiresInMinutes);
    ClaimsPrincipal? GetPrincipalFromToken(string token);
}
