using System.Diagnostics;

namespace AuthenticationService.API.Middlewares;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var request = context.Request;
        var requestPath = request.Path;
        var requestMethod = request.Method;

        _logger.LogInformation("➡️ [HTTP Request] {Method} {Path}{QueryString}", 
            requestMethod, 
            requestPath, 
            request.QueryString.HasValue ? request.QueryString.Value : string.Empty);

        try
        {
            await _next(context);

            stopwatch.Stop();
            var statusCode = context.Response.StatusCode;

            _logger.LogInformation("⬅️ [HTTP Response] {Method} {Path} -> Durum: {StatusCode} | Süre: {ElapsedMs} ms", 
                requestMethod, 
                requestPath, 
                statusCode, 
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "💥 [HTTP Error] {Method} {Path} işlenirken beklenmeyen hata oluştu! Süre: {ElapsedMs} ms", 
                requestMethod, 
                requestPath, 
                stopwatch.ElapsedMilliseconds);
            
            throw;
        }
    }
}
