using AuthenticationService.API.Middlewares;
using AuthenticationService.Core.Data;
using AuthenticationService.Core.Interfaces;
using AuthenticationService.Core.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Controller Servisleri
builder.Services.AddControllers();

// 2. Veritabanı (PostgreSQL)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("PostgreSQL bağlantı dizesi (DefaultConnection) bulunamadı.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// 3. Dependency Injection
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// 4. JWT Authentication
var jwtSecretKey = builder.Configuration["JwtSettings:SecretKey"]
    ?? throw new InvalidOperationException("JWT SecretKey bulunamadı.");
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "AuthenticationService.API";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "Mikroservis.Client";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// 5. OpenAPI, Swagger & Scalar
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 6. CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

// Veritabanını Başlat: Tablo oluştur ve varsayılan Admin kullanıcısını oluştur
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    try
    {
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "Users" (
                "Id" uuid NOT NULL PRIMARY KEY,
                "Username" character varying(50) NOT NULL,
                "Email" character varying(100) NOT NULL,
                "PasswordHash" text NOT NULL,
                "Role" character varying(20) NOT NULL DEFAULT 'User',
                "IsEmailConfirmed" boolean NOT NULL DEFAULT FALSE,
                "EmailConfirmationToken" text NULL,
                "EmailConfirmationTokenExpiresAt" timestamp with time zone NULL,
                "CurrentSessionToken" text NULL,
                "ProfilePictureUrl" text NULL,
                "University" text NULL,
                "CvUrl" text NULL,
                "AuthorApprovalStatus" character varying(20) NULL,
                "AuthorApplicationDate" timestamp with time zone NULL,
                "AuthorRejectionReason" text NULL,
                "PasswordResetToken" text NULL,
                "PasswordResetTokenExpiresAt" timestamp with time zone NULL,
                "IsBanned" boolean NOT NULL DEFAULT FALSE,
                "BannedUntil" timestamp with time zone NULL,
                "BanReason" text NULL,
                "AccountDeletionToken" text NULL,
                "AccountDeletionTokenExpiresAt" timestamp with time zone NULL,
                "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                "LastLoginAt" timestamp with time zone NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Email" ON "Users" ("Email");
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Username" ON "Users" ("Username");
            ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "University" text NULL;
            ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "CvUrl" text NULL;
            ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "AuthorApprovalStatus" character varying(20) NULL;
            ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "AuthorApplicationDate" timestamp with time zone NULL;
            ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "AuthorRejectionReason" text NULL;
            ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "PasswordResetToken" text NULL;
            ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "PasswordResetTokenExpiresAt" timestamp with time zone NULL;
            ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "IsBanned" boolean NOT NULL DEFAULT FALSE;
            ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "BannedUntil" timestamp with time zone NULL;
            ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "BanReason" text NULL;
            ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "AccountDeletionToken" text NULL;
            ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "AccountDeletionTokenExpiresAt" timestamp with time zone NULL;

            CREATE TABLE IF NOT EXISTS "UserNotifications" (
                "Id" uuid NOT NULL PRIMARY KEY,
                "UserId" uuid NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
                "Title" character varying(150) NOT NULL,
                "Message" text NOT NULL,
                "Type" character varying(30) NOT NULL DEFAULT 'Warning',
                "IsRead" boolean NOT NULL DEFAULT FALSE,
                "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc')
            );
            CREATE INDEX IF NOT EXISTS "IX_UserNotifications_UserId" ON "UserNotifications" ("UserId");
        """);

        app.Logger.LogInformation("Veritabanı tabloları ('Users', 'UserNotifications') hazır.");

        // Varsayılan Admin Seed & Doğrulama
        var existingAdmin = db.Users.FirstOrDefault(u => u.Username == "admin" || u.Email == "admin@lumina.com");
        if (existingAdmin == null)
        {
            db.Users.Add(new AuthenticationService.Core.Entities.User
            {
                Id = Guid.NewGuid(),
                Username = "admin",
                Email = "admin@lumina.com",
                PasswordHash = hasher.HashPassword("Admin123!*"),
                Role = "Admin",
                IsEmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            });
            db.SaveChanges();
            app.Logger.LogInformation("Varsayılan Admin (admin / admin@lumina.com / Admin123!*) oluşturuldu.");
        }
        else
        {
            existingAdmin.Role = "Admin";
            existingAdmin.IsEmailConfirmed = true;
            existingAdmin.IsBanned = false;
            existingAdmin.PasswordHash = hasher.HashPassword("Admin123!*");
            db.SaveChanges();
            app.Logger.LogInformation("Varsayılan Admin (admin / Admin123!*) güncellendi.");
        }
    }
    catch (Exception ex) { app.Logger.LogWarning(ex, "Veritabanı başlatma sırasında uyarı oluştu."); }
}

app.UseCors();
app.UseStaticFiles();

// İstek Loglama Middleware
app.UseMiddleware<RequestLoggingMiddleware>();

// OpenAPI, Swagger & Scalar
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

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
