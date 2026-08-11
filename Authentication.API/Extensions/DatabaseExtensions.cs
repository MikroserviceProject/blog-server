using AuthenticationService.Core.Data;
using AuthenticationService.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationService.API.Extensions;

public static class DatabaseExtensions
{
    public static void InitializeDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
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
                    "IsDeactivated" boolean NOT NULL DEFAULT FALSE,
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
                ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "IsDeactivated" boolean NOT NULL DEFAULT FALSE;

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
        catch (Exception ex) 
        { 
            app.Logger.LogWarning(ex, "Veritabanı başlatma sırasında uyarı oluştu."); 
        }
    }
}
