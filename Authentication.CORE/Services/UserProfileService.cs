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

public class UserProfileService : IUserProfileService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UserProfileService> _logger;
    private readonly IEmailService _emailService;

    public UserProfileService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger<UserProfileService> logger,
        IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _logger = logger;
        _emailService = emailService;
    }


    public async Task<ApiResponseDto<UserDto>> UpdateProfileAsync(Guid userId, UpdateProfileRequestDto request)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
        if (user == null)
        {
            return ApiResponseDto<UserDto>.Fail("Kullanıcı bulunamadı.");
        }

        if (user.IsBanned && (!user.BannedUntil.HasValue || user.BannedUntil.Value > DateTime.UtcNow))
        {
            return ApiResponseDto<UserDto>.Fail("Hesabınız askıya alınmıştır. Profil bilgilerinizi güncelleyemezsiniz.");
        }

        var usernameNormalized = request.Username.Trim().ToLower();
        var usernameTaken = await _unitOfWork.Repository<User>().Query().AnyAsync(u => u.Id != userId && u.Username.ToLower() == usernameNormalized);
        if (usernameTaken)
        {
            return ApiResponseDto<UserDto>.Fail("Bu kullanıcı adı başka bir üye tarafından kullanılıyor.");
        }

        var emailNormalized = request.Email.Trim().ToLower();
        var emailTaken = await _unitOfWork.Repository<User>().Query().AnyAsync(u => u.Id != userId && u.Email.ToLower() == emailNormalized);
        if (emailTaken)
        {
            return ApiResponseDto<UserDto>.Fail("Bu e-posta adresi başka bir hesap tarafından kullanılıyor.");
        }

        bool emailChanged = !string.Equals(user.Email, request.Email.Trim(), StringComparison.OrdinalIgnoreCase);
        user.Username = request.Username.Trim();
        if (request.ProfilePictureUrl != null)
        {
            user.ProfilePictureUrl = string.IsNullOrWhiteSpace(request.ProfilePictureUrl) ? null : request.ProfilePictureUrl;
        }

        string message = "Profil bilgileriniz başarıyla güncellendi.";

        if (emailChanged)
        {
            user.Email = request.Email.Trim();
            user.IsEmailConfirmed = false;

            var confirmationToken = Guid.NewGuid().ToString("N");
            user.EmailConfirmationToken = confirmationToken;
            user.EmailConfirmationTokenExpiresAt = DateTime.UtcNow.AddHours(24);
            
            // E-posta değiştiğinde oturumu sonlandır
            user.CurrentSessionToken = Guid.NewGuid().ToString();

            try
            {
                await _emailService.SendEmailConfirmationAsync(user.Email, user.Username, confirmationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Profil güncellemesi sonrası doğrulama e-postası gönderilirken hata oluştu: {Email}", user.Email);
            }

            message = "E-posta adresiniz değiştiği için yeni adresinize doğrulama bağlantısı gönderildi. Güvenliğiniz için oturumunuz kapatıldı, lütfen e-postanızı doğrulayarak tekrar giriş yapınız.";
        }

        await _unitOfWork.SaveChangesAsync();

        return ApiResponseDto<UserDto>.Ok(_mapper.Map<UserDto>(user), message);
    }


    public async Task<ApiResponseDto<UserDto>> UpdateAvatarAsync(Guid userId, string avatarUrl)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
        if (user == null)
        {
            return ApiResponseDto<UserDto>.Fail("Kullanıcı bulunamadı.");
        }

        if (user.IsBanned && (!user.BannedUntil.HasValue || user.BannedUntil.Value > DateTime.UtcNow))
        {
            return ApiResponseDto<UserDto>.Fail("Hesabınız askıya alınmıştır. Profil resmi güncelleyemezsiniz.");
        }

        user.ProfilePictureUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl;
        await _unitOfWork.SaveChangesAsync();

        return ApiResponseDto<UserDto>.Ok(_mapper.Map<UserDto>(user), "Profil resmi başarıyla güncellendi.");
    }

    public async Task<ApiResponseDto<bool>> ApplyForAuthorAsync(Guid userId, string university, string cvUrl)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
        if (user == null)
        {
            return ApiResponseDto<bool>.Fail("Kullanıcı bulunamadı.");
        }

        if (user.IsBanned && (!user.BannedUntil.HasValue || user.BannedUntil.Value > DateTime.UtcNow))
        {
            return ApiResponseDto<bool>.Fail("Hesabınız askıya alınmıştır. Yazar başvurusu yapamazsınız.");
        }

        if (user.Role == "Author" && user.AuthorApprovalStatus == "Approved")
        {
            return ApiResponseDto<bool>.Fail("Zaten onaylanmış bir yazar hesabınız var.");
        }

        if (user.AuthorApprovalStatus == "Pending")
        {
            return ApiResponseDto<bool>.Fail("Zaten değerlendirmede olan bir başvurunuz bulunuyor.");
        }

        user.University = university.Trim();
        user.CvUrl = cvUrl;
        user.AuthorApprovalStatus = "Pending";
        user.AuthorApplicationDate = DateTime.UtcNow;
        user.AuthorRejectionReason = null;

        await _unitOfWork.SaveChangesAsync();

        // Adminlere Bildirim ve E-Posta Gönder
        var adminUsers = await _unitOfWork.Repository<User>().Query().Where(u => u.Role == "Admin").ToListAsync();
        foreach (var admin in adminUsers)
        {
            _unitOfWork.Repository<UserNotification>().AddAsync(new UserNotification
            {
                UserId = admin.Id,
                Title = "Yeni Yazar Başvurusu",
                Message = $"{user.Username} adlı kullanıcı yazar olmak için başvurdu. İncelemenizi bekliyor.",
                Type = "Info",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            // Admin'e E-Posta Gönder (Hata alsa bile diğer adminlere devam etsin)
            try
            {
                await _emailService.SendNewAuthorApplicationToAdminAsync(admin.Email, admin.Username, user.Username, university, cvUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admine yazar başvuru e-postası gönderilirken hata oluştu: {Email}", admin.Email);
            }
        }
        await _unitOfWork.SaveChangesAsync();

        try
        {
            await _emailService.SendAuthorApplicationReceivedAsync(user.Email, user.Username);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Yazar başvuru alındı e-postası gönderilirken hata oluştu: {Email}", user.Email);
        }

        return ApiResponseDto<bool>.Ok(true, "Yazar başvurunuz başarıyla alındı. İnceleme sonrası e-posta ile bilgilendirileceksiniz.");
    }


    public async Task<ApiResponseDto<UserDto>> GetCurrentUserAsync(Guid userId, string? sessionToken = null)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
        if (user == null)
        {
            return ApiResponseDto<UserDto>.Fail("Kullanıcı bulunamadı.");
        }

        if (user.IsBanned && (!user.BannedUntil.HasValue || user.BannedUntil.Value > DateTime.UtcNow))
        {
            return ApiResponseDto<UserDto>.Fail("Oturum sonlandı - Sistem kurallarını ihlal ettiğiniz için hesabınız askıya alınmıştır.");
        }

        // Tekil aktif oturum kontrolü:
        if (!string.IsNullOrEmpty(user.CurrentSessionToken) && !string.IsNullOrEmpty(sessionToken) && user.CurrentSessionToken != sessionToken)
        {
            return ApiResponseDto<UserDto>.Fail("Oturumunuz başka bir cihazdan giriş yapıldığı veya çıkış yapıldığı için sonlandırıldı.");
        }

        var unreadCount = await _unitOfWork.Repository<UserNotification>().Query().CountAsync(n => n.UserId == userId && !n.IsRead);
        var userDto = _mapper.Map<UserDto>(user);
        userDto.UnreadNotificationCount = unreadCount;

        return ApiResponseDto<UserDto>.Ok(userDto);
    }


    public async Task<ApiResponseDto<bool>> ChangePasswordAsync(Guid userId, ChangePasswordRequestDto request)
    {
        var passwordErrors = ValidatePasswordStrength(request.NewPassword);
        if (passwordErrors.Count > 0)
        {
            return ApiResponseDto<bool>.Fail("Yeni şifre güvenlik gereksinimlerini karşılamıyor.", passwordErrors);
        }

        var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
        if (user == null)
        {
            return ApiResponseDto<bool>.Fail("Kullanıcı bulunamadı.");
        }

        var isCurrentValid = _passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash);
        if (!isCurrentValid)
        {
            return ApiResponseDto<bool>.Fail("Mevcut şifrenizi hatalı girdiniz.");
        }

        if (string.Equals(request.CurrentPassword, request.NewPassword) || _passwordHasher.VerifyPassword(request.NewPassword, user.PasswordHash))
        {
            return ApiResponseDto<bool>.Fail("Yeni şifreniz eski şifrenizle aynı olamaz. Lütfen farklı bir şifre belirleyiniz.");
        }

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponseDto<bool>.Ok(true, "Şifreniz başarıyla değiştirildi.");
    }


    public async Task<ApiResponseDto<List<UserNotificationDto>>> GetUserNotificationsAsync(Guid userId)
    {
        var notifications = await _unitOfWork.Repository<UserNotification>().Query()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .ToListAsync();

        var notificationDtos = _mapper.Map<List<UserNotificationDto>>(notifications);
        return ApiResponseDto<List<UserNotificationDto>>.Ok(notificationDtos);
    }


    public async Task<ApiResponseDto<bool>> MarkNotificationAsReadAsync(Guid userId, Guid notificationId)
    {
        var notification = await _unitOfWork.Repository<UserNotification>().Query()
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification != null)
        {
            notification.IsRead = true;
            await _unitOfWork.SaveChangesAsync();
        }

        return ApiResponseDto<bool>.Ok(true);
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



    public async Task<ApiResponseDto<bool>> DeactivateAccountAsync(Guid userId)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
        if (user == null)
            return ApiResponseDto<bool>.Fail("Kullanıcı bulunamadı.");

        user.IsDeactivated = true;
        user.CurrentSessionToken = null; // Aktif oturumu sonlandır
        
        await _unitOfWork.SaveChangesAsync();
        
        _logger.LogInformation("Kullanıcı hesabını dondurdu: {UserId}", userId);
        return ApiResponseDto<bool>.Ok(true, "Hesabınız başarıyla dondurulmuştur. Tekrar giriş yaptığınızda otomatik olarak aktive edilecektir.");
    }
}
