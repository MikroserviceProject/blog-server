using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthenticationService.Core.Entities;
using AuthenticationService.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AuthenticationService.Core.Services;

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(User user, out int expiresInMinutes)
    {
        var secretKey = _configuration["JwtSettings:SecretKey"] 
            ?? "VarsayilanCokGizliVeUzunBirJwtSecretKey1234567890!@#$%^&*";
        var issuer = _configuration["JwtSettings:Issuer"] ?? "AuthenticationService";
        var audience = _configuration["JwtSettings:Audience"] ?? "MikroservisApp";
        
        if (!int.TryParse(_configuration["JwtSettings:ExpirationInMinutes"], out expiresInMinutes))
        {
            expiresInMinutes = 60; // Varsayılan 60 dakika
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (!string.IsNullOrEmpty(user.CurrentSessionToken))
        {
            claims.Add(new Claim("session_token", user.CurrentSessionToken));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(expiresInMinutes),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public ClaimsPrincipal? GetPrincipalFromToken(string token)
    {
        var secretKey = _configuration["JwtSettings:SecretKey"] 
            ?? "VarsayilanCokGizliVeUzunBirJwtSecretKey1234567890!@#$%^&*";
        var issuer = _configuration["JwtSettings:Issuer"] ?? "AuthenticationService";
        var audience = _configuration["JwtSettings:Audience"] ?? "MikroservisApp";

        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }
}
