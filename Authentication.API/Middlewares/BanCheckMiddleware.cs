using AuthenticationService.Core.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using AuthenticationService.Core.DTOs;

namespace AuthenticationService.API.Middlewares
{
    public class BanCheckMiddleware
    {
        private readonly RequestDelegate _next;

        public BanCheckMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (Guid.TryParse(userIdClaim, out var userId))
                {
                    var userStatus = await dbContext.Users
                        .Where(u => u.Id == userId)
                        .Select(u => new { u.IsBanned, u.BannedUntil })
                        .FirstOrDefaultAsync();

                    if (userStatus != null && userStatus.IsBanned)
                    {
                        if (!userStatus.BannedUntil.HasValue || userStatus.BannedUntil.Value > DateTime.UtcNow)
                        {
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            context.Response.ContentType = "application/json";
                            
                            var response = ApiResponseDto<object>.Fail("Hesabınız yasaklıdır. Sisteme erişiminiz engellenmiştir.");
                            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                            return;
                        }
                    }
                }
            }

            await _next(context);
        }
    }
}
