using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthenticationService.Core.Data;
using AuthenticationService.Core.DTOs;
using AuthenticationService.Core.Entities;
using AuthenticationService.Core.Interfaces;
using AutoMapper;
using AuthenticationService.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace AuthenticationService.Core.Services;

public class AdminService : IAdminService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminService> _logger;
    private readonly IEmailService _emailService;
    private readonly IRealTimeNotificationService _realTimeService;

    public AdminService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger<AdminService> logger,
        IEmailService emailService,
        IRealTimeNotificationService realTimeService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _logger = logger;
        _emailService = emailService;
        _realTimeService = realTimeService;
    }


    public async Task<ApiResponseDto<List<AuthorApplicationDto>>> GetAuthorApplicationsAsync()
    {
        var authors = await _unitOfWork.Repository<User>().Query()
            .Where(u => u.Role == "Author" || u.AuthorApprovalStatus != null)
            .OrderByDescending(u => u.AuthorApplicationDate ?? u.CreatedAt)
            .ToListAsync();

        var authorDtos = _mapper.Map<List<AuthorApplicationDto>>(authors);
        
        // Ensure some fallback defaults logic inside AutoMapper or apply here if necessary.
        // We'll trust our map config.
        foreach(var dto in authorDtos) {
            dto.AuthorApprovalStatus ??= "Pending";
        }

        return ApiResponseDto<List<AuthorApplicationDto>>.Ok(authorDtos);
    }


    public async Task<ApiResponseDto<bool>> ApproveAuthorApplicationAsync(Guid authorId)
    {
        var user = await _unitOfWork.Repository<User>().Query().FirstOrDefaultAsync(u => u.Id == authorId && (u.Role == "Author" || u.AuthorApprovalStatus != null));
        if (user == null)
        {
            return ApiResponseDto<bool>.Fail("Yazar başvurusu bulunamadı.");
        }

        user.Role = "Author";
        user.AuthorApprovalStatus = "Approved";
        user.AuthorRejectionReason = null;

        var confirmationToken = Guid.NewGuid().ToString("N");
        user.EmailConfirmationToken = confirmationToken;
        user.EmailConfirmationTokenExpiresAt = DateTime.UtcNow.AddDays(7);

        var notification = new UserNotification
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = "✅ Yazar Başvurunuz Onaylandı",
            Message = "Tebrikler! Yazar başvurunuz kabul edildi. Sisteme tam erişim sağlamak için lütfen e-postanıza gönderilen bağlantıya tıklayarak onaylayın.",
            Type = "Success",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        await _unitOfWork.Repository<UserNotification>().AddAsync(notification);

        await _unitOfWork.SaveChangesAsync();

        await _realTimeService.SendNewNotificationAsync(user.Id.ToString());

        try
        {
            await _emailService.SendAuthorApprovedAsync(user.Email, user.Username, confirmationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Yazar onay e-postası gönderilirken hata oluştu: {Email}", user.Email);
        }

        return ApiResponseDto<bool>.Ok(true, $"'{user.Username}' kullanıcısının yazar başvurusu onaylandı ve aktivasyon e-postası gönderildi.");
    }


    public async Task<ApiResponseDto<bool>> RejectAuthorApplicationAsync(Guid authorId, string? reason)
    {
        var user = await _unitOfWork.Repository<User>().Query().FirstOrDefaultAsync(u => u.Id == authorId && (u.Role == "Author" || u.AuthorApprovalStatus != null));
        if (user == null)
        {
            return ApiResponseDto<bool>.Fail("Yazar başvurusu bulunamadı.");
        }

        user.AuthorApprovalStatus = "Rejected";
        user.AuthorRejectionReason = reason;

        var notification = new UserNotification
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = "⛔ Yazar Başvurunuz Reddedildi",
            Message = $"Yazar başvurunuz maalesef onaylanmamıştır. Gerekçe: {(string.IsNullOrWhiteSpace(reason) ? "Belirtilmedi" : reason)}",
            Type = "Warning",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        await _unitOfWork.Repository<UserNotification>().AddAsync(notification);

        await _unitOfWork.SaveChangesAsync();

        await _realTimeService.SendNewNotificationAsync(user.Id.ToString());

        try
        {
            await _emailService.SendAuthorRejectedAsync(user.Email, user.Username, reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Yazar red e-postası gönderilirken hata oluştu: {Email}", user.Email);
        }

        return ApiResponseDto<bool>.Ok(true, $"'{user.Username}' kullanıcısının yazar başvurusu reddedildi.");
    }


    public async Task<ApiResponseDto<UserDto>> CreateAdminAsync(CreateAdminRequestDto request)
    {
        // 0. Yönetici Güvenlik Anahtarı kontrolü
        var expectedAdminKey = _configuration["AdminSecretKey"] ?? "LuminaAdmin2026!*";
        if (string.IsNullOrWhiteSpace(request.AdminSecretKey) || request.AdminSecretKey != expectedAdminKey)
        {
            return ApiResponseDto<UserDto>.Fail("Geçersiz yönetici güvenlik anahtarı (AdminSecretKey).");
        }

        // 1. Şifre kuralları kontrolü
        var passwordErrors = ValidatePasswordStrength(request.Password);
        if (passwordErrors.Count > 0)
        {
            return ApiResponseDto<UserDto>.Fail("Şifre güvenlik gereksinimlerini karşılamıyor.", passwordErrors);
        }

        // 2. E-posta benzersizlik kontrolü
        var emailNormalized = request.Email.Trim().ToLower();
        var exists = await _unitOfWork.Repository<User>().Query().AnyAsync(u => u.Email.ToLower() == emailNormalized);
        if (exists)
        {
            return ApiResponseDto<UserDto>.Fail("Bu e-posta adresi zaten sistemde kayıtlı.");
        }

        // 3. Kullanıcı adı benzersizlik kontrolü
        var usernameNormalized = request.Username.Trim().ToLower();
        var usernameExists = await _unitOfWork.Repository<User>().Query().AnyAsync(u => u.Username.ToLower() == usernameNormalized);
        if (usernameExists)
        {
            return ApiResponseDto<UserDto>.Fail("Bu kullanıcı adı zaten alınmış.");
        }

        // 4. Şifreyi hashle ve Admin olarak kaydet (E-posta onaylı olarak açılır)
        var passwordHash = _passwordHasher.HashPassword(request.Password);
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username.Trim(),
            Email = emailNormalized,
            PasswordHash = passwordHash,
            Role = "Admin",
            AuthorApprovalStatus = "Approved",
            IsEmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<User>().AddAsync(adminUser);
        await _unitOfWork.SaveChangesAsync();

        var userDto = _mapper.Map<UserDto>(adminUser);
        return ApiResponseDto<UserDto>.Ok(userDto, $"'{adminUser.Username}' kullanıcısı başarıyla Yönetici (Admin) olarak oluşturuldu.");
    }


    public async Task<ApiResponseDto<PaginatedResultDto<UserDto>>> GetAllUsersAsync(int pageNumber = 1, int pageSize = 10, string? searchTerm = null)
    {
        var query = _unitOfWork.Repository<User>().Query();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(u => EF.Functions.ILike(u.Username, $"%{searchTerm}%") || EF.Functions.ILike(u.Email, $"%{searchTerm}%"));
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = users.Select(u => _mapper.Map<UserDto>(u)).ToList();

        var result = new PaginatedResultDto<UserDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            TotalPages = totalPages,
            CurrentPage = pageNumber,
            PageSize = pageSize
        };

        return ApiResponseDto<PaginatedResultDto<UserDto>>.Ok(result);
    }


    public async Task<ApiResponseDto<bool>> BanUserAsync(BanUserRequestDto request)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(request.UserId);
        if (user == null)
        {
            return ApiResponseDto<bool>.Fail("Kullanıcı bulunamadı.");
        }

        if (user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            return ApiResponseDto<bool>.Fail("Yönetici (Admin) hesapları askıya alınamaz veya banlanamaz.");
        }

        var reason = request.GetEffectiveReason();
        if (string.IsNullOrWhiteSpace(reason))
        {
            return ApiResponseDto<bool>.Fail("Lütfen bir banlama / askıya alma gerekçesi belirtiniz.");
        }

        DateTime? bannedUntil = request.BannedUntil;
        if (!bannedUntil.HasValue && request.DurationMinutes.HasValue && request.DurationMinutes.Value > 0)
        {
            bannedUntil = DateTime.UtcNow.AddMinutes(request.DurationMinutes.Value);
        }

        user.IsBanned = true;
        user.BannedUntil = bannedUntil;
        user.BanReason = reason;


        await _unitOfWork.SaveChangesAsync();

        // Anında kullanıcıyı dışarı atmak için SignalR bildirimi gönder
        await _realTimeService.SendUserBannedAsync(user.Id.ToString(), $"Hesabınız askıya alındı. Sebep: {reason}");

        // Kullanıcıya e-posta gönder
        try
        {
            await _emailService.SendUserBannedEmailAsync(user.Email, user.Username, reason, bannedUntil);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ban bilgilendirme e-postası gönderilirken hata oluştu: {Email}", user.Email);
        }

        var durationMsg = bannedUntil.HasValue 
            ? $"{bannedUntil.Value:dd.MM.yyyy HH:mm} tarihine kadar" 
            : "süresiz olarak";

        return ApiResponseDto<bool>.Ok(true, $"'{user.Username}' kullanıcısı {durationMsg} askıya alındı ve bilgilendirme e-postası gönderildi.");
    }


    public async Task<ApiResponseDto<bool>> UnbanUserAsync(Guid userId)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
        if (user == null)
        {
            return ApiResponseDto<bool>.Fail("Kullanıcı bulunamadı.");
        }

        user.IsBanned = false;
        user.BannedUntil = null;
        user.BanReason = null;

        var notification = new UserNotification
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = "✅ Hesabınızın Engeli Kaldırıldı",
            Message = "Hesabınızın erişim engeli yönetici tarafından kaldırılmıştır. Platformu kullanmaya devam edebilirsiniz.",
            Type = "Info",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        await _unitOfWork.Repository<UserNotification>().AddAsync(notification);

        await _unitOfWork.SaveChangesAsync();

        await _realTimeService.SendNewNotificationAsync(user.Id.ToString());

        return ApiResponseDto<bool>.Ok(true, $"'{user.Username}' kullanıcısının engeli başarıyla kaldırıldı.");
    }


    public async Task<ApiResponseDto<bool>> SendAdminNotificationAsync(AdminSendNotificationDto request)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(request.UserId);
        if (user == null)
        {
            return ApiResponseDto<bool>.Fail("Kullanıcı bulunamadı.");
        }

        var notification = new UserNotification
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = request.Title.Trim(),
            Message = request.Message.Trim(),
            Type = string.IsNullOrWhiteSpace(request.Type) ? "Warning" : request.Type.Trim(),
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<UserNotification>().AddAsync(notification);
        await _unitOfWork.SaveChangesAsync();

        await _realTimeService.SendNewNotificationAsync(user.Id.ToString());

        return ApiResponseDto<bool>.Ok(true, $"'{user.Username}' kullanıcısına sistem içi bildirim iletildi.");
    }
    private static List<string> ValidatePasswordStrength(string password)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add("Şifre boş olamaz.");
            return errors;
        }

        if (password.Length < 8)
            errors.Add("Şifre en az 8 karakter olmalıdır.");

        if (!password.Any(char.IsUpper))
            errors.Add("Şifre en az bir büyük harf (A-Z) içermelidir.");

        if (!password.Any(char.IsLower))
            errors.Add("Şifre en az bir küçük harf (a-z) içermelidir.");

        if (!password.Any(char.IsDigit))
            errors.Add("Şifre en az bir rakam (0-9) içermelidir.");

        if (!password.Any(c => !char.IsLetterOrDigit(c)))
            errors.Add("Şifre en az bir özel karakter (!@#$%^&* vb.) içermelidir.");

        return errors;
    }

    private static UserDto MapToUserDto(User user)
    {
        bool isBannedActive = user.IsBanned;
        if (isBannedActive && user.BannedUntil.HasValue && user.BannedUntil.Value < DateTime.UtcNow)
        {
            isBannedActive = false;
        }

        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            ProfilePictureUrl = user.ProfilePictureUrl,
            University = user.University,
            CvUrl = user.CvUrl,
            AuthorApprovalStatus = user.AuthorApprovalStatus,
            AuthorApplicationDate = user.AuthorApplicationDate,
            IsEmailConfirmed = user.IsEmailConfirmed,
            IsBanned = isBannedActive,
            BannedUntil = user.BannedUntil,
            BanReason = user.BanReason,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };
    }
}
