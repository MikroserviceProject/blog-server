using AuthenticationService.API.Extensions;
using AuthenticationService.API.Middlewares;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Register Application Services (Controllers, DB, DI, AutoMapper)
builder.Services.AddApplicationServices(builder.Configuration);

// 2. Register JWT Authentication & Authorization
builder.Services.AddJwtAuthentication(builder.Configuration);

// 3. OpenAPI, Swagger & Scalar
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 4. CORS
builder.Services.AddCorsPolicy();

var app = builder.Build();

// 5. Initialize Database (Create tables & Seed default Admin)
app.InitializeDatabase();

app.UseCors();
app.UseStaticFiles();

// 6. Request Logging Middleware
app.UseMiddleware<RequestLoggingMiddleware>();

// 7. OpenAPI, Swagger & Scalar UI Configuration
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Authentication API v1");
        options.RoutePrefix = "swagger";
    });
    
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Authentication Service API")
               .WithTheme(ScalarTheme.Moon)
               .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

// 8. Auth & Middlewares
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<BanCheckMiddleware>();

// 9. Endpoints
app.MapControllers();

app.Run();
