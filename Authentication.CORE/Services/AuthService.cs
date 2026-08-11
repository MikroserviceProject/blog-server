using AuthenticationService.Core.Data;
using AuthenticationService.Core.DTOs;
using AuthenticationService.Core.Entities;
using AuthenticationService.Core.Interfaces;
using AutoMapper;
using AuthenticationService.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AuthenticationService.Core.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    // Doğrudan kayıt formu ile seçilebilen roller (Admin güvenlik nedeniyle hariçtir)
    private static readonly HashSet<string> AllowedPublicRegisterRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "User", "Author"
    };

    public AuthService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IPasswordHasher passwordHasher,
        IJwtService jwtService,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ApiResponseDto<UserDto>> RegisterAsync(RegisterRequestDto request)
    {
        // 1. Şifre güçlülük kontrolü (backend tarafı ikinci katman doğrulama)
        var passwordErrors = ValidatePasswordStrength(request.Password);
        if (passwordErrors.Count > 0)
        {
            return ApiResponseDto<UserDto>.Fail("Şifre güvenlik gereksinimlerini karşılamıyor.", passwordErrors);
        }

        // 2. Rol doğrulama (Admin güvenlik kısıtı)
        var roleInput = string.IsNullOrWhiteSpace(request.Role) ? "User" : request.Role.Trim();
        if (roleInput.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            return ApiResponseDto<UserDto>.Fail("Yönetici (Admin) hesabı doğrudan kayıt formuyla açılamaz. Yöneticiler yalnızca sistem yöneticisi tarafından yetkilendirilebilir.");
        }

        if (!AllowedPublicRegisterRoles.Contains(roleInput))
        {
            return ApiResponseDto<UserDto>.Fail($"Geçersiz üyelik planı: '{roleInput}'. Kayıt için geçerli planlar: User (Okur), Author (Yazar).");
        }

        // Normalize rol
        var role = AllowedPublicRegisterRoles.First(r => r.Equals(roleInput, StringComparison.OrdinalIgnoreCase));
        bool isAuthor = role.Equals("Author", StringComparison.OrdinalIgnoreCase);

        // Yazar için Üniversite kontrolü
        if (isAuthor && string.IsNullOrWhiteSpace(request.University))
        {
            return ApiResponseDto<UserDto>.Fail("Yazar başvurusu için lütfen mezun olduğunuz üniversiteyi belirtiniz.");
        }

        // 3. E-posta benzersizlik kontrolü
        var emailNormalized = request.Email.Trim().ToLower();
        var exists = await _unitOfWork.Repository<User>().Query().AnyAsync(u => u.Email.ToLower() == emailNormalized);
        if (exists)
        {
            return ApiResponseDto<UserDto>.Fail("Bu e-posta adresi zaten sistemde kayıtlı.");
        }

        // 4. Kullanıcı adı benzersizlik kontrolü
        var usernameNormalized = request.Username.Trim().ToLower();
        var usernameExists = await _unitOfWork.Repository<User>().Query().AnyAsync(u => u.Username.ToLower() == usernameNormalized);
        if (usernameExists)
        {
            return ApiResponseDto<UserDto>.Fail("Bu kullanıcı adı zaten alınmış.");
        }

        // 5. Şifre hashleme ve kullanıcı oluşturma
        var passwordHash = _passwordHasher.HashPassword(request.Password);
        var confirmationToken = Guid.NewGuid().ToString("N");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username.Trim(),
            Email = emailNormalized,
            PasswordHash = passwordHash,
            Role = role,
            University = isAuthor ? request.University?.Trim() : null,
            CvUrl = isAuthor ? request.CvUrl?.Trim() : null,
            AuthorApprovalStatus = isAuthor ? "Pending" : "Approved",
            AuthorApplicationDate = isAuthor ? DateTime.UtcNow : null,
            IsEmailConfirmed = false,
            EmailConfirmationToken = confirmationToken,
            EmailConfirmationTokenExpiresAt = DateTime.UtcNow.AddHours(24),
            CreatedAt = DateTime.UtcNow
        };

        // Önce E-posta gönderimini dene
        bool emailSent = false;
        if (isAuthor)
        {
            emailSent = await _emailService.SendAuthorApplicationReceivedAsync(user.Email, user.Username);
        }
        else
        {
            emailSent = await _emailService.SendEmailConfirmationAsync(user.Email, user.Username, confirmationToken);
        }

        if (!emailSent)
        {
            return ApiResponseDto<UserDto>.Fail("Girdiğiniz e-posta adresine ulaşılamadı. Lütfen geçerli (var olan) bir e-posta adresi girdiğinizden emin olunuz.");
        }

        // E-posta gönderimi başarılıysa kullanıcıyı veritabanına kaydet
        _unitOfWork.Repository<User>().AddAsync(user);

        // Adminlere Bildirim Gönder (Eğer başvuru Yazar olarak yapıldıysa)
        if (isAuthor)
        {
            var adminUsers = await _unitOfWork.Repository<User>().Query().Where(u => u.Role == "Admin").ToListAsync();
            foreach (var admin in adminUsers)
            {
                _unitOfWork.Repository<UserNotification>().AddAsync(new UserNotification
                {
                    UserId = admin.Id,
                    Title = "Yeni Yazar Başvurusu",
                    Message = $"{user.Username} adlı kullanıcı yeni yazar hesabıyla platforma kayıt oldu. Onayınızı bekliyor.",
                    Type = "Info",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await _unitOfWork.SaveChangesAsync();

        var userDto = _mapper.Map<UserDto>(user);
        if (isAuthor)
        {
            return ApiResponseDto<UserDto>.Ok(userDto, "Yazar başvurunuz başarıyla alındı! Sistem yöneticisinin incelemesinin ardından onay e-postası ve aktivasyon bağlantınız iletilecektir.");
        }
        else
        {
            return ApiResponseDto<UserDto>.Ok(userDto, "Kayıt başarıyla oluşturuldu! E-posta adresinize tek tıkla doğrulama bağlantısı gönderildi.");
        }
    }

    public async Task<ApiResponseDto<LoginResponseDto>> LoginAsync(LoginRequestDto request)
    {
        var identifier = request.EmailOrUsername.Trim().ToLower();

        var user = await _unitOfWork.Repository<User>().Query().FirstOrDefaultAsync(u => 
            u.Email.ToLower() == identifier || u.Username.ToLower() == identifier);

        if (user == null)
        {
            return ApiResponseDto<LoginResponseDto>.Fail("Kullanıcı adı veya şifre hatalı.");
        }

        var isValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
        if (!isValid)
        {
            return ApiResponseDto<LoginResponseDto>.Fail("Kullanıcı adı veya şifre hatalı.");
        }

        // Yazar Başvuru Durumu Kontrolü
        if (user.Role.Equals("Author", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(user.AuthorApprovalStatus, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                return ApiResponseDto<LoginResponseDto>.Fail("Yazar başvurunuz sistem yöneticisi onayında beklemektedir. Başvurunuz onaylandığında e-posta adresinize aktivasyon bağlantısı iletilecektir.");
            }
            if (string.Equals(user.AuthorApprovalStatus, "Rejected", StringComparison.OrdinalIgnoreCase))
            {
                var reason = string.IsNullOrWhiteSpace(user.AuthorRejectionReason) ? "" : $" (Gerekçe: {user.AuthorRejectionReason})";
                return ApiResponseDto<LoginResponseDto>.Fail($"Yazar başvurunuz onaylanmamıştır.{reason}");
            }
        }

        // Ban Kontrolü (Süre dolmuşsa otomatik kaldır)
        if (user.IsBanned)
        {
            if (user.BannedUntil.HasValue && user.BannedUntil.Value < DateTime.UtcNow)
            {
                // Süresi dolan ban otomatik kaldırılır
                user.IsBanned = false;
                user.BannedUntil = null;
                user.BanReason = null;
                await _unitOfWork.SaveChangesAsync();
            }
            else
            {
                var isoDate = user.BannedUntil.HasValue
                    ? user.BannedUntil.Value.ToString("o")
                    : "PERMANENT";
                return ApiResponseDto<LoginResponseDto>.Fail($"BANNED_UNTIL|{isoDate}|Hesabınız kuralları ihlal ettiği için askıya alınmıştır.");
            }
        }

        // Hesap Dondurma Kontrolü (Giriş yaptıysa tekrar açılır)
        if (user.IsDeactivated)
        {
            user.IsDeactivated = false;
            await _unitOfWork.SaveChangesAsync();
            // Dondurma kalktığını ayrıca loglamak eklenebilir.
        }

        // E-Posta Doğrulama Kontrolü (Tüm kullanıcılar ve onaylanmış yazarlar için zorunludur)
        if (!user.IsEmailConfirmed)
        {
            return ApiResponseDto<LoginResponseDto>.Fail("Giriş yapabilmek için lütfen önce e-posta adresinize gönderilen aktivasyon / doğrulama bağlantısına tıklayarak hesabınızı onaylayınız.");
        }

        // Tekil oturum token'ı üret
        var sessionToken = Guid.NewGuid().ToString("N");
        user.CurrentSessionToken = sessionToken;
        user.LastLoginAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        // JWT token üretimi
        var token = _jwtService.GenerateToken(user, out var expiresInMinutes);

        var unreadCount = await _unitOfWork.Repository<UserNotification>().Query().CountAsync(n => n.UserId == user.Id && !n.IsRead);
        var userDto = _mapper.Map<UserDto>(user);
        userDto.UnreadNotificationCount = unreadCount;

        var response = new LoginResponseDto
        {
            Token = token,
            TokenType = "Bearer",
            ExpiresInMinutes = expiresInMinutes,
            User = userDto
        };

        return ApiResponseDto<LoginResponseDto>.Ok(response, "Giriş başarılı.");
    }

    public async Task<ApiResponseDto<bool>> ConfirmEmailAsync(ConfirmEmailDto request)
    {
        var emailNormalized = request.Email.Trim().ToLower();
        var user = await _unitOfWork.Repository<User>().Query().FirstOrDefaultAsync(u => u.Email.ToLower() == emailNormalized);

        if (user == null)
        {
            return ApiResponseDto<bool>.Fail("Kullanıcı bulunamadı veya hesap silinmiş.");
        }

        if (user.IsEmailConfirmed)
        {
            return ApiResponseDto<bool>.Ok(true, "E-posta adresi zaten daha önce doğrulanmış.");
        }

        // Yazar ise onay kontrolü
        if (user.Role.Equals("Author", StringComparison.OrdinalIgnoreCase) && 
            !string.Equals(user.AuthorApprovalStatus, "Approved", StringComparison.OrdinalIgnoreCase))
        {
            return ApiResponseDto<bool>.Fail("Yazar başvurunuz henüz sistem yöneticisi tarafından onaylanmamıştır. Başvurunuz onaylandıktan sonra e-postanızı doğrulayabilirsiniz.");
        }

        var tokenInput = request.Token.Trim();
        bool isDevMasterCode = false;
        bool isTokenMatch = !string.IsNullOrWhiteSpace(user.EmailConfirmationToken) && 
                            string.Equals(user.EmailConfirmationToken.Trim(), tokenInput, StringComparison.OrdinalIgnoreCase);

        if (!isTokenMatch && !isDevMasterCode)
        {
            return ApiResponseDto<bool>.Fail("Doğrulama bağlantısı geçersiz. Lütfen en son gelen e-postadaki butona tıklayınız.");
        }

        if (!isDevMasterCode && user.EmailConfirmationTokenExpiresAt.HasValue && user.EmailConfirmationTokenExpiresAt.Value < DateTime.UtcNow)
        {
            return ApiResponseDto<bool>.Fail("Doğrulama bağlantısının geçerlilik süresi dolmuş. Lütfen yeni bir bağlantı talep ediniz.");
        }

        user.IsEmailConfirmed = true;
        user.EmailConfirmationToken = null;
        user.EmailConfirmationTokenExpiresAt = null;

        await _unitOfWork.SaveChangesAsync();

        return ApiResponseDto<bool>.Ok(true, "Tebrikler! E-posta adresiniz başarıyla doğrulandı.");
    }

    public async Task<ApiResponseDto<bool>> ResendConfirmationEmailAsync(string email)
    {
        var emailNormalized = email.Trim().ToLower();
        var user = await _unitOfWork.Repository<User>().Query().FirstOrDefaultAsync(u => u.Email.ToLower() == emailNormalized);

        if (user == null)
        {
            return ApiResponseDto<bool>.Fail("Kullanıcı bulunamadı.");
        }

        if (user.IsEmailConfirmed)
        {
            return ApiResponseDto<bool>.Ok(true, "E-posta adresi zaten doğrulanmış durumda.");
        }

        // Yazar ise ve onaylanmamışsa
        if (user.Role.Equals("Author", StringComparison.OrdinalIgnoreCase) && 
            !string.Equals(user.AuthorApprovalStatus, "Approved", StringComparison.OrdinalIgnoreCase))
        {
            return ApiResponseDto<bool>.Fail("Yazar başvurunuz henüz yönetici onayında beklemektedir. Başvurunuz onaylandığında aktivasyon e-postası otomatik olarak gönderilecektir.");
        }

        var confirmationToken = Guid.NewGuid().ToString("N");
        user.EmailConfirmationToken = confirmationToken;
        user.EmailConfirmationTokenExpiresAt = DateTime.UtcNow.AddHours(24);

        await _unitOfWork.SaveChangesAsync();

        try
        {
            if (user.Role.Equals("Author", StringComparison.OrdinalIgnoreCase))
            {
                await _emailService.SendAuthorApprovedAsync(user.Email, user.Username, confirmationToken);
            }
            else
            {
                await _emailService.SendEmailConfirmationAsync(user.Email, user.Username, confirmationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Doğrulama e-postası gönderilirken hata oluştu: {Email}", user.Email);
        }

        return ApiResponseDto<bool>.Ok(true, "Yeni doğrulama bağlantısı e-posta adresinize gönderildi.");
    }

    public async Task<ApiResponseDto<bool>> LogoutAsync(Guid userId)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
        if (user != null)
        {
            user.CurrentSessionToken = Guid.NewGuid().ToString();
            await _unitOfWork.SaveChangesAsync();
        }

        return ApiResponseDto<bool>.Ok(true, "Başarıyla çıkış yapıldı. Tüm cihazlardaki oturumlar sonlandırıldı.");
    }

    public async Task<ApiResponseDto<bool>> ForgotPasswordAsync(ForgotPasswordRequestDto request)
    {
        var emailNormalized = request.Email.Trim().ToLower();
        var user = await _unitOfWork.Repository<User>().Query().FirstOrDefaultAsync(u => u.Email.ToLower() == emailNormalized);

        if (user == null)
        {
            // Güvenlik gereği kullanıcı yoksa bile aynı başarılı yanıtı döneriz
            return ApiResponseDto<bool>.Ok(true, "Eğer bu e-posta sistemde kayıtlı ise şifre sıfırlama bağlantısı gönderilmiştir.");
        }

        var resetToken = Guid.NewGuid().ToString("N");
        user.PasswordResetToken = resetToken;
        user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(24);

        await _unitOfWork.SaveChangesAsync();

        try
        {
            await _emailService.SendPasswordResetAsync(user.Email, user.Username, resetToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Şifre sıfırlama e-postası gönderilirken hata oluştu: {Email}", user.Email);
        }

        return ApiResponseDto<bool>.Ok(true, "Şifre sıfırlama bağlantısı e-posta adresinize gönderildi.");
    }

    public async Task<ApiResponseDto<bool>> ResetPasswordAsync(ResetPasswordRequestDto request)
    {
        var passwordErrors = ValidatePasswordStrength(request.NewPassword);
        if (passwordErrors.Count > 0)
        {
            return ApiResponseDto<bool>.Fail("Şifre güvenlik gereksinimlerini karşılamıyor.", passwordErrors);
        }

        var emailNormalized = request.Email.Trim().ToLower();
        var user = await _unitOfWork.Repository<User>().Query().FirstOrDefaultAsync(u => u.Email.ToLower() == emailNormalized);

        if (user == null)
        {
            return ApiResponseDto<bool>.Fail("Geçersiz şifre sıfırlama talebi.");
        }

        var tokenInput = request.Token.Trim();
        bool isDevMasterCode = false;
        bool isTokenMatch = !string.IsNullOrWhiteSpace(user.PasswordResetToken) &&
                            string.Equals(user.PasswordResetToken.Trim(), tokenInput, StringComparison.OrdinalIgnoreCase);

        if (!isTokenMatch && !isDevMasterCode)
        {
            return ApiResponseDto<bool>.Fail("Şifre sıfırlama bağlantısı geçersiz veya daha önce kullanılmış.");
        }

        if (!isDevMasterCode && user.PasswordResetTokenExpiresAt.HasValue && user.PasswordResetTokenExpiresAt.Value < DateTime.UtcNow)
        {
            return ApiResponseDto<bool>.Fail("Şifre sıfırlama bağlantısının geçerlilik süresi dolmuş. Lütfen yeni bir talepte bulunun.");
        }

        if (_passwordHasher.VerifyPassword(request.NewPassword, user.PasswordHash))
        {
            return ApiResponseDto<bool>.Fail("Yeni şifreniz mevcut şifrenizle aynı olamaz. Lütfen farklı bir şifre belirleyiniz.");
        }

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiresAt = null;
        // Tüm oturumları sonlandır
        user.CurrentSessionToken = Guid.NewGuid().ToString();

        await _unitOfWork.SaveChangesAsync();

        return ApiResponseDto<bool>.Ok(true, "Şifreniz başarıyla güncellendi! Yeni şifrenizle giriş yapabilirsiniz.");
    }

    public async Task<ApiResponseDto<bool>> RequestAccountDeletionAsync(Guid userId)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
        if (user == null)
        {
            return ApiResponseDto<bool>.Fail("Kullanıcı bulunamadı.");
        }

        var deletionToken = Guid.NewGuid().ToString("N");
        user.AccountDeletionToken = deletionToken;
        user.AccountDeletionTokenExpiresAt = DateTime.UtcNow.AddHours(1);

        await _unitOfWork.SaveChangesAsync();

        try
        {
            await _emailService.SendAccountDeletionConfirmationAsync(user.Email, user.Username, deletionToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hesap silme onay e-postası gönderilirken hata oluştu: {Email}", user.Email);
            return ApiResponseDto<bool>.Fail("Hesap silme e-postası gönderilirken bir hata oluştu. Lütfen daha sonra tekrar deneyiniz.");
        }

        return ApiResponseDto<bool>.Ok(true, "Hesap silme onay bağlantısı e-posta adresinize gönderildi. Lütfen e-postanızı kontrol ederek silme işlemini onaylayınız.");
    }

    public async Task<ApiResponseDto<bool>> ConfirmAccountDeletionAsync(ConfirmAccountDeletionDto request)
    {
        var tokenInput = (request.Token ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(tokenInput))
        {
            return ApiResponseDto<bool>.Fail("Hesap silme onay anahtarı (token) gereklidir.");
        }

        User? user = null;
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var emailNormalized = request.Email.Trim().ToLower();
            user = await _unitOfWork.Repository<User>().Query().FirstOrDefaultAsync(u => u.Email.ToLower() == emailNormalized);
        }

        if (user == null)
        {
            user = await _unitOfWork.Repository<User>().Query().FirstOrDefaultAsync(u => u.AccountDeletionToken == tokenInput);
        }

        if (user == null)
        {
            return ApiResponseDto<bool>.Fail("Kullanıcı bulunamadı veya hesap daha önce silinmiş.");
        }

        bool isDevMasterCode = false;
        bool isTokenMatch = !string.IsNullOrWhiteSpace(user.AccountDeletionToken) &&
                            string.Equals(user.AccountDeletionToken.Trim(), tokenInput, StringComparison.OrdinalIgnoreCase);

        if (!isTokenMatch && !isDevMasterCode)
        {
            return ApiResponseDto<bool>.Fail("Hesap silme bağlantısı geçersiz veya süresi dolmuş.");
        }

        if (!isDevMasterCode && user.AccountDeletionTokenExpiresAt.HasValue && user.AccountDeletionTokenExpiresAt.Value < DateTime.UtcNow)
        {
            return ApiResponseDto<bool>.Fail("Hesap silme bağlantısının süresi dolmuş. Lütfen profilinizden tekrar silme talebinde bulununuz.");
        }

        // Kullanıcının bildirimlerini temizle
        var userNotifications = await _unitOfWork.Repository<UserNotification>().Query().Where(n => n.UserId == user.Id).ToListAsync();
        if (userNotifications.Count > 0)
        {
            _unitOfWork.Repository<UserNotification>().RemoveRange(userNotifications);
        }

        // Kullanıcıyı veritabanından kalıcı olarak sil
        _unitOfWork.Repository<User>().Remove(user);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponseDto<bool>.Ok(true, "Hesabınız başarıyla kalıcı olarak silindi. Tüm bilgileriniz temizlenmiştir.");
    }

    public async Task<ApiResponseDto<bool>> DeactivateAccountAsync(Guid userId)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
        if (user == null)
        {
            return ApiResponseDto<bool>.Fail("Kullanıcı bulunamadı.");
        }

        if (user.IsDeactivated)
        {
            return ApiResponseDto<bool>.Ok(true, "Hesabınız zaten dondurulmuş durumda.");
        }

        user.IsDeactivated = true;
        // Tüm oturumları sonlandırarak çıkış yapmasını sağla
        user.CurrentSessionToken = Guid.NewGuid().ToString();

        await _unitOfWork.SaveChangesAsync();

        return ApiResponseDto<bool>.Ok(true, "Hesabınız başarıyla donduruldu. Sisteme tekrar giriş yapana kadar profiliniz gizli kalacaktır.");
    }

    public async Task<ApiResponseDto<bool>> SendSupportRequestAsync(Guid userId, SupportRequestDto request)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
        if (user == null)
        {
            return ApiResponseDto<bool>.Fail("Kullanıcı bulunamadı.");
        }

        var admins = await _unitOfWork.Repository<User>().Query().Where(u => u.Role == "Admin").ToListAsync();

        foreach (var admin in admins)
        {
            var notification = new UserNotification
            {
                Id = Guid.NewGuid(),
                UserId = admin.Id,
                Title = $"Yeni {request.Type} Bildirimi - {user.Username}",
                Message = $"Kullanıcı ({user.Email}) yeni bir {request.Type} bildiriminde bulundu: {request.Message}",
                Type = "Info",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Repository<UserNotification>().AddAsync(notification);
            
            try
            {
                await _emailService.SendEmailAsync(admin.Email, $"Yeni {request.Type} Bildirimi - {user.Username}",
                    $"<h3>{user.Username} ({user.Email}) kullanıcısından yeni bir {request.Type} bildirimi geldi.</h3><p><strong>Mesaj:</strong> {request.Message}</p>");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Destek talebi e-postası gönderilirken hata oluştu: {Email}", admin.Email);
            }
        }

        await _unitOfWork.SaveChangesAsync();

        return ApiResponseDto<bool>.Ok(true, "Talebiniz başarıyla iletildi. En kısa sürede incelenecektir.");
    }

    /// <summary>
    /// Şifre güçlülük kurallarını backend tarafında doğrular.
    /// </summary>
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


}
