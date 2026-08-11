using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using BlogSite.CORE.Data;
using BlogSite.CORE.Mapping;
using BlogSite.CORE.Repositories.Abstract;
using BlogSite.CORE.Repositories.Concrete;
using BlogSite.CORE.Services;

namespace BlogSite.API.Extensions
{
    public static class BlogApiServiceExtensions
    {
        public const string DevCorsPolicy = "DevCorsPolicy";

        /// <summary>Controllers, Swagger, veritabanı, Repository/Service ve AutoMapper kayıtlarının tamamı.</summary>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddControllers()
                .AddJsonOptions(options =>
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

            services.AddOpenApi();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            services.AddDbContext<BlogDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("BlogDb")));

            services.AddScoped<IPostRepository, PostRepository>();
            services.AddScoped<IPostService, PostService>();
            services.AddAutoMapper(typeof(MappingProfile));

            return services;
        }

        /// <summary>Authentication.API tarafından üretilen JWT token'larını doğrulayacak şekilde kurar.</summary>
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSecretKey = configuration["JwtSettings:SecretKey"]
                ?? throw new InvalidOperationException("JWT SecretKey bulunamadı.");
            var jwtIssuer = configuration["JwtSettings:Issuer"] ?? "AuthenticationService.API";
            var jwtAudience = configuration["JwtSettings:Audience"] ?? "Mikroservis.Client";

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                // Authentication.API token'ı ClaimTypes.NameIdentifier (uzun URI) ile üretiyor; JWT'de bu
                // kısa forma ("nameid") sıkıştırılıyor. Doğrulama sırasında bunu tekrar uzun forma çevirmesi
                // için bunu açıkça true yapıyoruz, yoksa User.FindFirstValue(ClaimTypes.NameIdentifier) null dönebilir.
                options.MapInboundClaims = true;
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

            services.AddAuthorization();

            return services;
        }

        /// <summary>Geliştirme ortamı için her kaynaktan gelen isteklere izin veren gevşek bir CORS politikası.</summary>
        /// <remarks>TODO: Production'a çıkmadan önce belirli origin'lerle kısıtlanmalı.</remarks>
        public static IServiceCollection AddDevCorsPolicy(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy(DevCorsPolicy, policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            return services;
        }
    }
}
