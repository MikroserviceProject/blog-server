using System.Text;
using AuthenticationService.API.Middlewares;
using AuthenticationService.Core.Data;
using AuthenticationService.Core.Interfaces;
using AuthenticationService.Core.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Controller Servisleri
builder.Services.AddControllers();

// 2. Veritabanı (PostgreSQL)
var usePostgres = builder.Configuration.GetValue<bool>("DatabaseSettings:UsePostgreSQL", true);
var pgConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (usePostgres && !string.IsNullOrEmpty(pgConnectionString))
    {
        options.UseNpgsql(pgConnectionString);
    }
    else
    {
        options.UseSqlite("Data Source=authentication.db");
    }
});

// 3. Dependency Injection
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// 4. JWT Authentication
var jwtSecretKey = builder.Configuration["JwtSettings:SecretKey"] 
    ?? "MikroservisSuperGizliJwtGuvenlikAnahtari2026!@#$%^&*()_+";
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

// 5. OpenAPI
builder.Services.AddOpenApi();

// CORS (Frontend / Angular / Tarayıcı entegrasyonu için)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

// Otomatik Veritabanı Migrationlarını Uygula & Tabloyu Garanti Et & Varsayılan Yöneticiyi Seed Et
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    try
    {
        var provider = db.Database.ProviderName ?? "";
        bool isPostgres = provider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgre", StringComparison.OrdinalIgnoreCase);

        // 1. Önce EF Core Migration'ı dene
        try
        {
            db.Database.Migrate();
        }
        catch (Exception migEx)
        {
            Console.WriteLine($"[Migration Uyarısı]: {migEx.Message}");
        }

        // 2. Tablo eğer pgAdmin'den elle silindiyse veya yeni sütunlar gerekiyorsa otomatik olarak güncelle (Self-Healing)
        try
        {
            if (isPostgres)
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
                        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                        "LastLoginAt" timestamp with time zone NULL
                    );

                    CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Email" ON "Users" ("Email");
                    CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Username" ON "Users" ("Username");

                    -- Mevcut tabloda eksik olabilecek yeni sütunları otomatik ekle
                    ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "University" text NULL;
                    ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "CvUrl" text NULL;
                    ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "AuthorApprovalStatus" character varying(20) NULL;
                    ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "AuthorApplicationDate" timestamp with time zone NULL;
                    ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "AuthorRejectionReason" text NULL;
                    ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "PasswordResetToken" text NULL;
                    ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "PasswordResetTokenExpiresAt" timestamp with time zone NULL;
                """);
            }
            else
            {
                db.Database.ExecuteSqlRaw("""
                    CREATE TABLE IF NOT EXISTS "Users" (
                        "Id" TEXT NOT NULL PRIMARY KEY,
                        "Username" TEXT NOT NULL,
                        "Email" TEXT NOT NULL,
                        "PasswordHash" TEXT NOT NULL,
                        "Role" TEXT NOT NULL DEFAULT 'User',
                        "IsEmailConfirmed" INTEGER NOT NULL DEFAULT 0,
                        "EmailConfirmationToken" TEXT NULL,
                        "EmailConfirmationTokenExpiresAt" TEXT NULL,
                        "CurrentSessionToken" TEXT NULL,
                        "ProfilePictureUrl" TEXT NULL,
                        "University" TEXT NULL,
                        "CvUrl" TEXT NULL,
                        "AuthorApprovalStatus" TEXT NULL,
                        "AuthorApplicationDate" TEXT NULL,
                        "AuthorRejectionReason" TEXT NULL,
                        "PasswordResetToken" TEXT NULL,
                        "PasswordResetTokenExpiresAt" TEXT NULL,
                        "CreatedAt" TEXT NOT NULL,
                        "LastLoginAt" TEXT NULL
                    );

                    CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Email" ON "Users" ("Email");
                    CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Username" ON "Users" ("Username");
                """);

                // SQLite sütun kontrolleri
                var columns = new[] { "University", "CvUrl", "AuthorApprovalStatus", "AuthorApplicationDate", "AuthorRejectionReason", "PasswordResetToken", "PasswordResetTokenExpiresAt" };
                foreach (var col in columns)
                {
                    try { db.Database.ExecuteSqlRaw($"ALTER TABLE Users ADD COLUMN {col} TEXT NULL;"); } catch { }
                }
            }
            Console.WriteLine("✅ [Veritabanı]: 'Users' tablosu ve sütunları doğrulandı / hazır.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Tablo Kontrol Uyarısı]: {ex.Message}");
        }

        // 3. Özel Tanımlı Yönetici (Admin) Seed Kontrolü
        try
        {
            var adminExists = db.Users.Any(u => u.Role == "Admin");
            if (!adminExists)
            {
                var adminUser = new AuthenticationService.Core.Entities.User
                {
                    Id = Guid.NewGuid(),
                    Username = "admin",
                    Email = "admin@lumina.com",
                    PasswordHash = hasher.HashPassword("Admin123!*"),
                    Role = "Admin",
                    IsEmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };
                db.Users.Add(adminUser);
                db.SaveChanges();
                Console.WriteLine("✅ [Sistem Bilgisi]: Varsayılan Sistem Yöneticisi (admin / Admin123!*) oluşturuldu.");
            }
        }
        catch (Exception seedEx)
        {
            Console.WriteLine($"[Admin Seed Uyarısı]: {seedEx.Message}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Genel Veritabanı Uyarısı]: {ex.Message}");
    }
}

app.UseCors();
app.UseStaticFiles();

// 6. Özel İstek Loglama Middleware'i
app.UseMiddleware<RequestLoggingMiddleware>();

// 7. OpenAPI & Scalar UI
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Authentication Service API")
               .WithTheme(ScalarTheme.Moon)
               .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.UseAuthentication();
app.UseAuthorization();

// 8. İnteraktif Canlı Test Dashboard'u (Ana Sayfa: http://localhost:5001)
app.MapGet("/", () => Results.Content(GetDashboardHtml(), "text/html", Encoding.UTF8));

app.MapControllers();

app.Run();

static string GetDashboardHtml()
{
    return """
<!DOCTYPE html>
<html lang="tr">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>🛡️ Authentication Service | Canlı Yönetim & Test Paneli</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700;800&family=JetBrains+Mono:wght@400;500;600&display=swap" rel="stylesheet">
    <style>
        :root {
            --bg-base: #0a0e17;
            --bg-card: #111827;
            --bg-card-hover: #1f293d;
            --bg-input: #1b2436;
            --border: #243048;
            --border-highlight: #38bdf8;
            --accent: #38bdf8;
            --accent-gradient: linear-gradient(135deg, #38bdf8 0%, #818cf8 100%);
            --accent-hover: #0ea5e9;
            --purple: #a855f7;
            --success: #10b981;
            --warning: #f59e0b;
            --danger: #ef4444;
            --text-main: #f8fafc;
            --text-muted: #94a3b8;
            --font-sans: 'Inter', system-ui, -apple-system, sans-serif;
            --font-mono: 'JetBrains Mono', monospace;
        }

        * { box-sizing: border-box; margin: 0; padding: 0; }
        body {
            background-color: var(--bg-base);
            color: var(--text-main);
            font-family: var(--font-sans);
            padding: 24px;
            min-height: 100vh;
        }

        .header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding-bottom: 20px;
            border-bottom: 1px solid var(--border);
            margin-bottom: 24px;
        }
        .header-title {
            font-size: 22px;
            font-weight: 800;
            background: var(--accent-gradient);
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
            display: flex;
            align-items: center;
            gap: 10px;
        }
        .service-badges { display: flex; gap: 10px; }
        .badge {
            display: inline-flex;
            align-items: center;
            gap: 6px;
            padding: 6px 12px;
            border-radius: 9999px;
            font-size: 12px;
            font-weight: 600;
            text-decoration: none;
            transition: all 0.2s;
        }
        .badge-auth { background: rgba(56, 189, 248, 0.12); color: var(--accent); border: 1px solid rgba(56, 189, 248, 0.3); }
        .badge-db { background: rgba(16, 185, 129, 0.12); color: var(--success); border: 1px solid rgba(16, 185, 129, 0.3); }
        .badge:hover { transform: translateY(-1px); filter: brightness(1.2); }
        .dot { width: 8px; height: 8px; border-radius: 50%; display: inline-block; }
        .dot-auth { background: var(--accent); animation: pulse 2s infinite; }
        .dot-db { background: var(--success); }
        @keyframes pulse { 0%, 100% { opacity: 1; } 50% { opacity: 0.4; } }

        .main-nav {
            display: flex;
            gap: 12px;
            margin-bottom: 20px;
            background: var(--bg-card);
            padding: 6px;
            border-radius: 12px;
            border: 1px solid var(--border);
            width: fit-content;
        }
        .main-nav-btn {
            background: transparent;
            border: none;
            color: var(--text-muted);
            padding: 10px 20px;
            border-radius: 8px;
            cursor: pointer;
            font-weight: 600;
            font-size: 14px;
            display: flex;
            align-items: center;
            gap: 8px;
            transition: all 0.2s;
        }
        .main-nav-btn.active {
            background: var(--accent-gradient);
            color: #0b0f19;
            box-shadow: 0 4px 12px rgba(56, 189, 248, 0.25);
        }

        .grid {
            display: grid;
            grid-template-columns: 1.1fr 0.9fr;
            gap: 24px;
        }
        @media(max-width: 1024px) { .grid { grid-template-columns: 1fr; } }
        .card {
            background: var(--bg-card);
            border: 1px solid var(--border);
            border-radius: 14px;
            padding: 22px;
            box-shadow: 0 10px 30px rgba(0,0,0,0.3);
        }
        .card-header {
            font-size: 15px;
            font-weight: 700;
            margin-bottom: 18px;
            display: flex;
            align-items: center;
            justify-content: space-between;
            border-bottom: 1px solid var(--border);
            padding-bottom: 14px;
        }
        .sub-tabs { display: flex; gap: 8px; margin-bottom: 18px; flex-wrap: wrap; }
        .sub-tab-btn {
            background: var(--bg-input);
            border: 1px solid var(--border);
            color: var(--text-muted);
            padding: 8px 14px;
            border-radius: 8px;
            cursor: pointer;
            font-weight: 500;
            font-size: 13px;
            transition: all 0.2s;
        }
        .sub-tab-btn.active {
            background: var(--accent);
            color: #0b0f19;
            border-color: var(--accent);
            font-weight: 600;
        }
        .form-group { margin-bottom: 14px; }
        label { display: block; font-size: 12px; font-weight: 600; color: var(--text-muted); margin-bottom: 6px; text-transform: uppercase; letter-spacing: 0.5px; }
        input, select, textarea {
            width: 100%;
            background: var(--bg-input);
            border: 1px solid var(--border);
            color: #fff;
            padding: 10px 14px;
            border-radius: 8px;
            font-size: 14px;
            outline: none;
            transition: border-color 0.2s;
            font-family: inherit;
        }
        input:focus, select:focus, textarea:focus { border-color: var(--accent); }
        .form-row { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }

        .btn {
            width: 100%;
            background: #2563eb;
            color: #fff;
            padding: 12px;
            border: none;
            border-radius: 8px;
            font-size: 14px;
            font-weight: 600;
            cursor: pointer;
            transition: all 0.2s;
            display: flex;
            align-items: center;
            justify-content: center;
            gap: 8px;
        }
        .btn:hover { background: #1d4ed8; transform: translateY(-1px); }
        .btn-purple { background: #9333ea; }
        .btn-purple:hover { background: #7e22ce; }
        .btn-success { background: #059669; }
        .btn-success:hover { background: #047857; }
        .btn-danger { background: #dc2626; }
        .btn-danger:hover { background: #b91c1c; }

        .session-info {
            background: rgba(56, 189, 248, 0.08);
            border: 1px solid rgba(56, 189, 248, 0.2);
            border-radius: 10px;
            padding: 14px;
            margin-top: 16px;
            font-size: 12px;
        }
        .session-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 8px; }
        .session-user { font-weight: 700; color: var(--accent); font-size: 13px; }
        .token-text {
            word-break: break-all;
            font-family: var(--font-mono);
            color: #94a3b8;
            font-size: 11px;
            max-height: 52px;
            overflow-y: auto;
            background: var(--bg-base);
            padding: 6px 8px;
            border-radius: 6px;
            margin-top: 4px;
        }

        .console-body {
            background: #060911;
            border: 1px solid var(--border);
            border-radius: 10px;
            padding: 16px;
            font-family: var(--font-mono);
            font-size: 12px;
            color: #38bdf8;
            overflow-y: auto;
            height: 480px;
            white-space: pre-wrap;
            word-break: break-all;
        }
        .status-ok { color: var(--success); }
        .status-err { color: var(--danger); }
        .tag {
            padding: 3px 8px;
            border-radius: 6px;
            font-size: 11px;
            font-weight: 700;
            text-transform: uppercase;
        }
        .tag-auth { background: rgba(56, 189, 248, 0.2); color: #38bdf8; border: 1px solid rgba(56, 189, 248, 0.4); }
    </style>
</head>
<body>
    <div class="header">
        <div class="header-title">
            <span>🛡️ Authentication Service</span>
            <span style="font-size: 14px; color: var(--text-muted); font-weight: 400;">| Giriş, Kayıt, Rol & JWT Yönetimi</span>
        </div>
        <div class="service-badges">
            <span class="badge badge-auth"><span class="dot dot-auth"></span> Auth API :5001</span>
            <span class="badge badge-db"><span class="dot dot-db"></span> PostgreSQL :5432</span>
            <a href="/scalar/v1" target="_blank" class="badge" style="background: rgba(168, 85, 247, 0.15); color: var(--purple); border: 1px solid rgba(168, 85, 247, 0.3);">📖 Scalar API Docs</a>
        </div>
    </div>

    <div class="main-nav">
        <button class="main-nav-btn active" onclick="switchMainTab('auth')">🔐 Kimlik İşlemleri (Auth)</button>
        <button class="main-nav-btn" onclick="switchMainTab('db')">👥 PostgreSQL Kullanıcılar Tablosu</button>
    </div>

    <div class="grid">
        <!-- SOL PANEL -->
        <div class="card">
            <!-- 1. AUTH SEKMESİ -->
            <div id="main-tab-auth">
                <div class="card-header">
                    <span>⚡ İşlem Seçiniz</span>
                </div>
                <div class="sub-tabs">
                    <button class="sub-tab-btn active" onclick="switchAuthTab('register')">📝 1. Kayıt Ol (Register)</button>
                    <button class="sub-tab-btn" onclick="switchAuthTab('confirm')">✉️ 2. Mail Onayla (Confirm)</button>
                    <button class="sub-tab-btn" onclick="switchAuthTab('login')">🔐 3. Giriş Yap (Login)</button>
                    <button class="sub-tab-btn" onclick="switchAuthTab('me')">👤 4. Profilim & Token (Me)</button>
                </div>

                <!-- 1. REGISTER -->
                <div id="auth-tab-register">
                    <div class="form-group">
                        <label>Kullanıcı Adı</label>
                        <input type="text" id="reg-username" value="saliha_yazar" placeholder="Kullanıcı adı giriniz (harf, rakam, _ )">
                    </div>
                    <div class="form-group">
                        <label>E-Posta Adresi</label>
                        <input type="email" id="reg-email" value="sahilcicek44@gmail.com" placeholder="ornek@alanadi.com">
                    </div>
                    <div class="form-row">
                        <div class="form-group">
                            <label>Şifre</label>
                            <input type="password" id="reg-password" value="GucluSifre123!">
                            <div style="font-size: 10px; color: var(--text-muted); margin-top: 4px;">
                                Min 8 karakter, 1 büyük harf, 1 küçük harf, 1 rakam, 1 özel karakter
                            </div>
                        </div>
                        <div class="form-group">
                            <label>Üyelik Planı</label>
                            <select id="reg-role">
                                <option value="Author" selected>✍️ Yazar (Premium - ₺99/ay)</option>
                                <option value="User">👤 Okur (Ücretsiz)</option>
                            </select>
                        </div>
                    </div>
                    <button class="btn" onclick="handleRegister()">📝 Kayıt Ol (POST /api/auth/register)</button>
                </div>

                <!-- 2. CONFIRM EMAIL -->
                <div id="auth-tab-confirm" style="display: none;">
                    <div class="form-group">
                        <label>E-Posta Adresi</label>
                        <input type="email" id="conf-email" value="saliha@example.com">
                    </div>
                    <div class="form-group">
                        <label>6 Haneli Onay Kodu</label>
                        <input type="text" id="conf-token" placeholder="Konsoldan gelen 6 haneli kodu yazın (örn: 123456)">
                    </div>
                    <button class="btn btn-purple" onclick="handleConfirmEmail()">✉️ E-Postayı Doğrula (POST /api/auth/confirm-email)</button>
                </div>

                <!-- 3. LOGIN -->
                <div id="auth-tab-login" style="display: none;">
                    <div class="form-group">
                        <label>E-Posta veya Kullanıcı Adı</label>
                        <input type="text" id="login-identifier" value="saliha@example.com">
                    </div>
                    <div class="form-group">
                        <label>Şifre</label>
                        <input type="password" id="login-password" value="GucluSifre123!">
                    </div>
                    <button class="btn btn-success" onclick="handleLogin()">🔐 Giriş Yap & JWT Token Al (POST /api/auth/login)</button>
                </div>

                <!-- 4. PROFILE / ME -->
                <div id="auth-tab-me" style="display: none;">
                    <p style="color: var(--text-muted); font-size: 13px; margin-bottom: 16px;">
                        Bu istek JWT Bearer Token gerektirir. Giriş yapıldığında token otomatik olarak <code>Authorization: Bearer [TOKEN]</code> başlığına eklenir.
                    </p>
                    <button class="btn" onclick="handleGetMe()">👤 Bilgilerimi Getir (GET /api/auth/me)</button>
                </div>

                <!-- AKTİF OTURUM KARTI -->
                <div class="session-info" id="session-box" style="display: none;">
                    <div class="session-header">
                        <div>
                            <span>Oturum Açık: </span>
                            <span class="session-user" id="session-user-name">-</span>
                            <span class="tag tag-auth" id="session-user-role">User</span>
                        </div>
                        <button class="btn btn-danger" style="width: auto; padding: 4px 10px; font-size: 11px;" onclick="handleLogout()">Çıkış Yap</button>
                    </div>
                    <div style="font-size: 11px; color: var(--text-muted);">Aktif JWT Token:</div>
                    <div class="token-text" id="session-token-preview">-</div>
                </div>
            </div>

            <!-- 2. POSTGRESQL DB SEKMESİ -->
            <div id="main-tab-db" style="display: none;">
                <div class="card-header">
                    <span>👥 PostgreSQL Canlı Tablo Verileri</span>
                    <button class="btn" style="width: auto; padding: 6px 12px; font-size: 12px;" onclick="loadDbTables()">🔄 Yenile</button>
                </div>
                <div id="db-content">
                    <p style="color: var(--text-muted); font-size: 13px;">Yükleniyor...</p>
                </div>
            </div>
        </div>

        <!-- SAĞ PANEL: CANLI LOG KONSOLU -->
        <div class="card">
            <div class="card-header">
                <span>📡 API İstek & Yanıt Konsolu</span>
                <button class="btn" style="width: auto; padding: 4px 10px; font-size: 11px; background: var(--bg-input); border: 1px solid var(--border);" onclick="clearConsole()">Temizle</button>
            </div>
            <div class="console-body" id="console">🚀 Authentication Service Hazır!
İşlem yapmak için soldaki formu kullanabilir veya Scalar arayüzünü açabilirsiniz.
</div>
        </div>
    </div>

    <script>
        const AUTH_API = 'http://localhost:5001';
        let currentJwtToken = localStorage.getItem('auth_jwt_token') || '';
        let currentUserData = null;

        window.onload = () => {
            if (currentJwtToken) {
                document.getElementById('session-box').style.display = 'block';
                document.getElementById('session-token-preview').innerText = currentJwtToken;
                handleGetMe();
            }
        };

        function switchMainTab(tab) {
            document.querySelectorAll('.main-nav-btn').forEach((btn, idx) => {
                btn.classList.toggle('active', (tab === 'auth' && idx === 0) || (tab === 'db' && idx === 1));
            });
            document.getElementById('main-tab-auth').style.display = tab === 'auth' ? 'block' : 'none';
            document.getElementById('main-tab-db').style.display = tab === 'db' ? 'block' : 'none';

            if (tab === 'db') loadDbTables();
        }

        function switchAuthTab(tab) {
            document.querySelectorAll('.sub-tabs .sub-tab-btn').forEach((btn, idx) => {
                const tabs = ['register', 'confirm', 'login', 'me'];
                btn.classList.toggle('active', tabs[idx] === tab);
            });
            document.getElementById('auth-tab-register').style.display = tab === 'register' ? 'block' : 'none';
            document.getElementById('auth-tab-confirm').style.display = tab === 'confirm' ? 'block' : 'none';
            document.getElementById('auth-tab-login').style.display = tab === 'login' ? 'block' : 'none';
            document.getElementById('auth-tab-me').style.display = tab === 'me' ? 'block' : 'none';
        }

        function logConsole(title, data, isError = false) {
            const c = document.getElementById('console');
            const time = new Date().toLocaleTimeString();
            const colorClass = isError ? 'status-err' : 'status-ok';
            const logEntry = `\n[${time}] ${title}\n` + JSON.stringify(data, null, 2) + '\n' + '─'.repeat(45);
            c.innerText += logEntry;
            c.scrollTop = c.scrollHeight;
        }

        function clearConsole() {
            document.getElementById('console').innerText = '📡 Konsol temizlendi.\n';
        }

        // 1. REGISTER
        async function handleRegister() {
            const body = {
                username: document.getElementById('reg-username').value,
                email: document.getElementById('reg-email').value,
                password: document.getElementById('reg-password').value,
                role: document.getElementById('reg-role').value
            };

            try {
                const res = await fetch(`${AUTH_API}/api/auth/register`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(body)
                });
                const data = await res.json();
                logConsole('POST /api/auth/register', data, !res.ok);

                if (res.ok) {
                    document.getElementById('conf-email').value = body.email;
                    document.getElementById('login-identifier').value = body.email;
                    switchAuthTab('confirm');
                }
            } catch (err) {
                logConsole('Register Hatası', { message: err.message }, true);
            }
        }

        // 2. CONFIRM EMAIL
        async function handleConfirmEmail() {
            const body = {
                email: document.getElementById('conf-email').value,
                token: document.getElementById('conf-token').value
            };

            try {
                const res = await fetch(`${AUTH_API}/api/auth/confirm-email`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(body)
                });
                const data = await res.json();
                logConsole('POST /api/auth/confirm-email', data, !res.ok);

                if (res.ok) {
                    switchAuthTab('login');
                }
            } catch (err) {
                logConsole('Email Confirm Hatası', { message: err.message }, true);
            }
        }

        // 3. LOGIN
        async function handleLogin() {
            const body = {
                emailOrUsername: document.getElementById('login-identifier').value,
                password: document.getElementById('login-password').value
            };

            try {
                const res = await fetch(`${AUTH_API}/api/auth/login`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(body)
                });
                const data = await res.json();
                logConsole('POST /api/auth/login', data, !res.ok);

                if (res.ok && data.data) {
                    currentJwtToken = data.data.token;
                    currentUserData = data.data.user;
                    localStorage.setItem('auth_jwt_token', currentJwtToken);

                    document.getElementById('session-box').style.display = 'block';
                    document.getElementById('session-user-name').innerText = currentUserData.username;
                    document.getElementById('session-user-role').innerText = currentUserData.role;
                    document.getElementById('session-token-preview').innerText = currentJwtToken;
                    switchAuthTab('me');
                }
            } catch (err) {
                logConsole('Login Hatası', { message: err.message }, true);
            }
        }

        // 4. ME
        async function handleGetMe() {
            if (!currentJwtToken) {
                logConsole('Hata', { message: 'Önce giriş yapmalısınız (Token yok).' }, true);
                return;
            }

            try {
                const res = await fetch(`${AUTH_API}/api/auth/me`, {
                    headers: { 'Authorization': `Bearer ${currentJwtToken}` }
                });
                const data = await res.json();
                logConsole('GET /api/auth/me (Bearer Token ile)', data, !res.ok);

                if (res.ok && data.data) {
                    currentUserData = data.data;
                    document.getElementById('session-box').style.display = 'block';
                    document.getElementById('session-user-name').innerText = currentUserData.username;
                    document.getElementById('session-user-role').innerText = currentUserData.role;
                }
            } catch (err) {
                logConsole('Me Hatası', { message: err.message }, true);
            }
        }

        // LOGOUT
        function handleLogout() {
            currentJwtToken = '';
            currentUserData = null;
            localStorage.removeItem('auth_jwt_token');
            document.getElementById('session-box').style.display = 'none';
            logConsole('Oturum Kapatıldı', { message: 'JWT Token yerel hafızadan silindi.' });
            switchAuthTab('login');
        }

        // POSTGRESQL TABLES
        async function loadDbTables() {
            const el = document.getElementById('db-content');
            el.innerHTML = "<p style='color: var(--text-muted);'>PostgreSQL verileri çekiliyor...</p>";
            try {
                const resUsers = await fetch(`${AUTH_API}/api/auth/database-users`);
                const users = await resUsers.json();

                let html = `<h4 style="margin-bottom: 12px; color: var(--accent);">👥 Users Tablosu (${users.length} Kayıt - PostgreSQL: AuthenticationDb)</h4>`;
                html += `
                    <table style="width: 100%; border-collapse: collapse; margin-bottom: 24px; font-size: 13px;">
                        <thead>
                            <tr style="border-bottom: 1px solid var(--border); color: var(--text-muted); text-align: left;">
                                <th style="padding: 10px;">Kullanıcı</th>
                                <th style="padding: 10px;">E-Posta</th>
                                <th style="padding: 10px;">Rol</th>
                                <th style="padding: 10px;">Onay Durumu</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${users.map(u => `
                                <tr style="border-bottom: 1px solid var(--border);">
                                    <td style="padding: 10px; font-weight: 600;">${u.username}</td>
                                    <td style="padding: 10px; color: var(--text-muted);">${u.email}</td>
                                    <td style="padding: 10px;"><span class="tag tag-auth">${u.role}</span></td>
                                    <td style="padding: 10px;">${u.isEmailConfirmed ? '✅ Onaylandı' : '⏳ Kod Bekliyor'}</td>
                                </tr>
                            `).join('')}
                        </tbody>
                    </table>`;

                el.innerHTML = html;
                logConsole("PostgreSQL Canlı Tablo Verileri", { usersCount: users.length });
            } catch (err) {
                el.innerHTML = `<p style="color: var(--danger);">Veritabanı okunamadı: ${err.message}</p>`;
            }
        }
    </script>
</body>
</html>
""";
}
