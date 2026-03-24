using Project_Bloodwave_Backend.Data;
using Project_Bloodwave_Backend.DTOs;
using Project_Bloodwave_Backend.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;

namespace Project_Bloodwave_Backend.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
    Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
    Task<AuthResponseDto> LogoutAsync(int userId);
    Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto);
    Task<AuthResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto dto);
    Task<AuthResponseDto> ResetPasswordAsync(ResetPasswordRequestDto dto);
}

/// <summary>
/// Service responsible for user authentication and JWT token generation
/// </summary>
public class AuthService : IAuthService
{
    private readonly BloodwaveDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IMailService _mailService;
    private readonly ILogger<AuthService> _logger;
    
    private const string InvalidCredentialsMessage = "Invalid username or password";
    private const string DefaultJwtKey = "your-super-secret-key-that-must-be-at-least-32-characters-long-for-hmacsha256";
    private const string DefaultJwtIssuer = "BloodwaveApi";
    private const string DefaultJwtAudience = "BloodwaveClient";
    private const int TokenExpirationHours = 24;
    private const int RefreshTokenExpirationDays = 7;
    private const int PasswordResetExpirationMinutes = 30;
    private const int RefreshTokenByteLength = 64;
    private const string AdminRoleName = "Admin";
    private const string UserRoleName = "User";
    private const string RefreshTokenUserAgent = "refresh-token";
    private const string PasswordResetTokenUserAgent = "password-reset";
    private const string ForgotPasswordGenericMessage = "If the email exists in our system, a password reset link has been sent.";

    public AuthService(
        BloodwaveDbContext context,
        IConfiguration configuration,
        IMailService mailService,
        ILogger<AuthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _mailService = mailService;
        _logger = logger;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
    {
        var validationResult = ValidateInputAsync(registerDto);
        if (!validationResult.IsValid)
            return validationResult.ToResponse();

        var existingUserResult = await CheckExistingUserAsync(registerDto.Username, registerDto.Email);
        if (existingUserResult != null)
            return existingUserResult;

        var user = CreateUser(registerDto);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        await SendWelcomeEmailAsync(user);

        var refreshToken = await CreateRefreshTokenAsync(user.Id);

        return new AuthResponseDto
        {
            Success = true,
            Message = "User registered successfully",
            Token = GenerateJwtToken(user),
            RefreshToken = refreshToken.Token,
            ExpiresAt = refreshToken.ExpiresAt,
            User = MapToUserDto(user)
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == loginDto.Username);
        
        if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            return new AuthResponseDto { Success = false, Message = InvalidCredentialsMessage };

        if (!user.IsActive)
            return new AuthResponseDto { Success = false, Message = "User account is inactive" };

        var accessToken = GenerateJwtToken(user);
        var refreshToken = await CreateRefreshTokenAsync(user.Id);

        return new AuthResponseDto
        {
            Success = true,
            Message = "Login successful",
            Token = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresAt = DateTime.UtcNow.AddHours(TokenExpirationHours),
            User = MapToUserDto(user)
        };
    }

    public async Task<AuthResponseDto> LogoutAsync(int userId)
    {
        var refreshTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync();

        foreach (var refreshToken in refreshTokens)
        {
            refreshToken.RevokedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return new AuthResponseDto
        {
            Success = true,
            Message = "Logged out successfully"
        };
    }

    /// <summary>
    /// Generates a JWT token for the specified user
    /// </summary>
    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(GetJwtKeyBytes());
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var role = ResolveRole(user);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken(
            issuer: GetConfigValue("Jwt:Issuer", DefaultJwtIssuer),
            audience: GetConfigValue("Jwt:Audience", DefaultJwtAudience),
            claims: claims,
            expires: DateTime.UtcNow.AddHours(TokenExpirationHours),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Gets the JWT signing key from configuration
    /// </summary>
    private byte[] GetJwtKeyBytes()
    {
        var keyString = GetConfigValue("Jwt:Key", DefaultJwtKey);
        return Encoding.UTF8.GetBytes(keyString);
    }

    /// <summary>
    /// Creates and stores a refresh token for the user
    /// </summary>
    private async Task<RefreshToken> CreateRefreshTokenAsync(int userId)
    {
        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = GenerateRandomToken(),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpirationDays),
            CreatedByIp = "127.0.0.1",
            UserAgent = RefreshTokenUserAgent
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();
        return refreshToken;
    }

    /// <summary>
    /// Creates and stores a refresh token for the user, revoking the old one
    /// </summary>
    private async Task<RefreshToken> CreateRefreshTokenAsync(int userId, string oldToken)
    {
        // Revoke old token
        var oldRefreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt =>
                rt.Token == oldToken &&
                rt.UserId == userId &&
                rt.UserAgent != PasswordResetTokenUserAgent);
        
        if (oldRefreshToken != null)
        {
            oldRefreshToken.RevokedAt = DateTime.UtcNow;
        }

        var newRefreshToken = new RefreshToken
        {
            UserId = userId,
            Token = GenerateRandomToken(),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpirationDays),
            ReplacesToken = oldToken,
            CreatedByIp = "127.0.0.1",
            UserAgent = RefreshTokenUserAgent
        };

        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync();
        return newRefreshToken;
    }

    /// <summary>
    /// Generates a cryptographically secure random token
    /// </summary>
    private string GenerateRandomToken()
    {
        var randomBytes = new byte[RefreshTokenByteLength];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        return Convert.ToBase64String(randomBytes);
    }

    /// <summary>
    /// Creates a new user entity from registration data
    /// </summary>
    private User CreateUser(RegisterDto registerDto)
    {
        return new User
        {
            Username = registerDto.Username,
            Email = registerDto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Checks if user already exists by username or email
    /// </summary>
    private async Task<AuthResponseDto?> CheckExistingUserAsync(string username, string email)
    {
        if (await _context.Users.AnyAsync(u => u.Username == username))
            return new AuthResponseDto { Success = false, Message = "Username already exists" };

        if (await _context.Users.AnyAsync(u => u.Email == email))
            return new AuthResponseDto { Success = false, Message = "Email already registered" };

        return null;
    }

    /// <summary>
    /// Maps a user entity to user DTO
    /// </summary>
    private static UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            CreatedAt = user.CreatedAt
        };
    }

    /// <summary>
    /// Gets a configuration value with a default fallback
    /// </summary>
    private string GetConfigValue(string key, string defaultValue)
    {
        return _configuration[key] ?? defaultValue;
    }

    /// <summary>
    /// Resolves JWT role for the user from configuration.
    /// Admin users can be defined by username or email under Authorization section.
    /// </summary>
    private string ResolveRole(User user)
    {
        var adminUsernames = _configuration
            .GetSection("Authorization:AdminUsernames")
            .Get<string[]>() ?? Array.Empty<string>();

        if (adminUsernames.Any(u => string.Equals(u, user.Username, StringComparison.OrdinalIgnoreCase)))
            return AdminRoleName;

        var adminEmails = _configuration
            .GetSection("Authorization:AdminEmails")
            .Get<string[]>() ?? Array.Empty<string>();

        if (adminEmails.Any(e => string.Equals(e, user.Email, StringComparison.OrdinalIgnoreCase)))
            return AdminRoleName;

        return UserRoleName;
    }

    /// <summary>
    /// Validates registration input data
    /// </summary>
    private static RegistrationValidation ValidateInputAsync(RegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
            return new RegistrationValidation(false, "Username and password required");

        return new RegistrationValidation(true, null);
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenDto.RefreshToken))
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Refresh token is required"
            };
        }

        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt =>
                rt.Token == refreshTokenDto.RefreshToken &&
                rt.UserAgent != PasswordResetTokenUserAgent);

        if (refreshToken == null || !refreshToken.IsActive)
            return new AuthResponseDto 
            { 
                Success = false, 
                Message = "Invalid or expired refresh token" 
            };

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == refreshToken.UserId);
        if (user == null)
            return new AuthResponseDto 
            { 
                Success = false, 
                Message = "User not found" 
            };
        
        // Create new refresh token
        var newRefreshToken = await CreateRefreshTokenAsync(user.Id, refreshTokenDto.RefreshToken);

        return new AuthResponseDto
        {
            Success = true,
            Message = "Token refreshed successfully",
            Token = GenerateJwtToken(user),
            RefreshToken = newRefreshToken.Token,
            ExpiresAt = newRefreshToken.ExpiresAt,
            User = MapToUserDto(user)
        };
    }

    public async Task<AuthResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            return new AuthResponseDto { Success = true, Message = ForgotPasswordGenericMessage };

        var normalizedEmail = dto.Email.Trim();
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail.ToLower() && u.IsActive);

        if (user == null)
            return new AuthResponseDto { Success = true, Message = ForgotPasswordGenericMessage };

        var activeResetTokens = await _context.RefreshTokens
            .Where(rt =>
                rt.UserId == user.Id &&
                rt.RevokedAt == null &&
                rt.ExpiresAt > DateTime.UtcNow &&
                rt.UserAgent == PasswordResetTokenUserAgent)
            .ToListAsync();

        foreach (var token in activeResetTokens)
            token.RevokedAt = DateTime.UtcNow;

        var resetToken = new RefreshToken
        {
            UserId = user.Id,
            Token = GenerateRandomToken(),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(PasswordResetExpirationMinutes),
            CreatedByIp = "password-reset",
            UserAgent = PasswordResetTokenUserAgent
        };

        _context.RefreshTokens.Add(resetToken);
        await _context.SaveChangesAsync();

        var resetUrl = BuildResetUrl(resetToken.Token); // pl: https://.../reset-password?token=...
        var htmlBody = BuildPasswordResetEmailHtml(user.Username, resetUrl, PasswordResetExpirationMinutes);

        await TrySendEmailAsync(
            user.Email,
            "Bloodwave - Password reset",
            text: "",
            html: htmlBody
        );

        return new AuthResponseDto { Success = true, Message = ForgotPasswordGenericMessage };
    }

    private static string BuildPasswordResetEmailHtml(string username, string resetUrl, int expiresMinutes)
    {
        var safeUsername = WebUtility.HtmlEncode(username);
        var safeResetUrl = WebUtility.HtmlEncode(resetUrl);
        var bodyHtml = $@"
            <p>We received a password reset request for your account.</p>
            <p>This link will expire in <strong>{expiresMinutes} minutes</strong>.</p>
            <p style=""margin-top:14px;font-size:12px;color:#9a9a9a;word-break:break-all;"">{safeResetUrl}</p>
            <p style=""margin-top:14px;color:#b6b6b6;font-size:12px;"">If you did not request this, you can ignore this email.</p>";

        return BuildMinimalEmailHtml(
            title: "Bloodwave",
            subtitle: "Password Reset",
            greeting: $"Hello {safeUsername},",
            bodyHtml: bodyHtml,
            actionText: "Reset Password",
            actionUrl: resetUrl);
    }

    public async Task<AuthResponseDto> ResetPasswordAsync(ResetPasswordRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Token) || string.IsNullOrWhiteSpace(dto.NewPassword))
            return new AuthResponseDto { Success = false, Message = "Token and new password are required" };

        var resetToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt =>
                rt.Token == dto.Token &&
                rt.UserAgent == PasswordResetTokenUserAgent &&
                rt.RevokedAt == null &&
                rt.ExpiresAt > DateTime.UtcNow);

        if (resetToken == null)
            return new AuthResponseDto { Success = false, Message = "Invalid or expired reset token" };

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == resetToken.UserId && u.IsActive);
        if (user == null)
            return new AuthResponseDto { Success = false, Message = "User not found" };

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        resetToken.RevokedAt = DateTime.UtcNow;

        var activeLoginTokens = await _context.RefreshTokens
            .Where(rt =>
                rt.UserId == user.Id &&
                rt.RevokedAt == null &&
                rt.ExpiresAt > DateTime.UtcNow &&
                rt.UserAgent != PasswordResetTokenUserAgent)
            .ToListAsync();

        foreach (var token in activeLoginTokens)
            token.RevokedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var htmlBody = BuildPasswordChangedEmailHtml(user.Username);

        await TrySendEmailAsync(
            user.Email,
            "Bloodwave - Password changed",
            text: "",
            html: htmlBody
        );

        return new AuthResponseDto { Success = true, Message = "Password has been reset successfully" };
    }

    private static string BuildPasswordChangedEmailHtml(string username)
    {
        var safeUsername = WebUtility.HtmlEncode(username);

        var bodyHtml = @"
            <p>Your password was changed successfully.</p>
            <p style=""color:#b6b6b6;font-size:12px;"">If this was not you, secure your account and contact support immediately.</p>";

        return BuildMinimalEmailHtml(
            title: "Bloodwave",
            subtitle: "Security Notice",
            greeting: $"Hello {safeUsername},",
            bodyHtml: bodyHtml,
            actionText: "Contact Support",
            actionUrl: "https://bloodwave.game/support");
    }

    private async Task SendWelcomeEmailAsync(User user)
    {
        var htmlBody = BuildWelcomeEmailHtml(user.Username);

        await TrySendEmailAsync(
            user.Email,
            "Welcome to Bloodwave",
            text: "",
            html: htmlBody
        );
    }

    private static string BuildWelcomeEmailHtml(string username)
    {
        var safeUsername = WebUtility.HtmlEncode(username);

        var bodyHtml = @"
            <p>Your account is ready.</p>
            <p>Log in and start your first match.</p>";

        return BuildMinimalEmailHtml(
            title: "Bloodwave",
            subtitle: "Welcome",
            greeting: $"Hello {safeUsername},",
            bodyHtml: bodyHtml,
            actionText: "Open Bloodwave",
            actionUrl: "https://bloodwave.game/login");
    }

    private async Task TrySendEmailAsync(string to, string subject, string text = "", string? html = null)
    {
        try
        {
            var result = await _mailService.SendEmailAsync(to, subject, text, html);
            if (!result.IsSuccess)
                _logger.LogWarning("Email send failed. To={To}, Subject={Subject}, Error={Error}", to, subject, result.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected email send error. To={To}, Subject={Subject}", to, subject);
        }
    }

    private string BuildResetUrl(string token)
    {
        var baseUrl = _configuration["App:PasswordResetUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
            return token;

        var separator = baseUrl.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return $"{baseUrl}{separator}token={Uri.EscapeDataString(token)}";
    }

    public static string BuildEmailChangedEmailHtml(string username, string oldEmail, string newEmail)
    {
        var safeUsername = WebUtility.HtmlEncode(username);
        var safeOldEmail = WebUtility.HtmlEncode(oldEmail);
        var safeNewEmail = WebUtility.HtmlEncode(newEmail);

        var bodyHtml = $@"
            <p>Your account email was updated.</p>
            <p><strong>Old:</strong> {safeOldEmail}<br /><strong>New:</strong> {safeNewEmail}</p>
            <p style=""margin-top:14px;color:#b6b6b6;font-size:12px;"">If this was not you, contact support immediately.</p>";

        return BuildMinimalEmailHtml(
            title: "Bloodwave",
            subtitle: "Email Changed",
            greeting: $"Hello {safeUsername},",
            bodyHtml: bodyHtml,
            actionText: "Contact Support",
            actionUrl: "https://bloodwave.game/support");
    }

    public static string BuildAccountUpdatedEmailHtml(string username, string oldUsername, bool usernameChanged, bool passwordChanged)
    {
        var safeUsername = WebUtility.HtmlEncode(username);
        var safeOldUsername = WebUtility.HtmlEncode(oldUsername);

        var changes = new List<string>();
        if (usernameChanged)
            changes.Add($"<li><strong>Username:</strong> {safeOldUsername} -> {safeUsername}</li>");
        if (passwordChanged)
            changes.Add("<li><strong>Password:</strong> changed</li>");

        var changesHtml = changes.Count == 0
            ? "<p>No visible profile fields were changed.</p>"
            : $"<p>The following updates were made:</p><ul style=\"margin:8px 0 0 20px;padding:0;\">{string.Join(string.Empty, changes)}</ul>";

        var bodyHtml = $@"
            {changesHtml}
            <p style=""margin-top:14px;color:#b6b6b6;font-size:12px;"">If this was not you, secure your account immediately.</p>";

        return BuildMinimalEmailHtml(
            title: "Bloodwave",
            subtitle: "Account Update",
            greeting: $"Hello {safeUsername},",
            bodyHtml: bodyHtml,
            actionText: "Contact Support",
            actionUrl: "https://bloodwave.game/support");
    }

    private static string BuildMinimalEmailHtml(
        string title,
        string subtitle,
        string greeting,
        string bodyHtml,
        string? actionText = null,
        string? actionUrl = null)
    {
        var safeTitle = WebUtility.HtmlEncode(title);
        var safeSubtitle = WebUtility.HtmlEncode(subtitle);
        var safeGreeting = WebUtility.HtmlEncode(greeting);
        var safeActionText = string.IsNullOrWhiteSpace(actionText) ? null : WebUtility.HtmlEncode(actionText);
        var safeActionUrl = string.IsNullOrWhiteSpace(actionUrl) ? null : WebUtility.HtmlEncode(actionUrl);
        var actionBlock = safeActionText is null || safeActionUrl is null
            ? string.Empty
            : $@"<div style=""margin-top:24px;""><a href=""{safeActionUrl}"" style=""display:inline-block;background:#d4af37;color:#171717;text-decoration:none;padding:10px 18px;border-radius:6px;font-weight:600;font-size:13px;"">{safeActionText}</a></div>";

        return $@"<!DOCTYPE html>
<html lang=""en"" xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <meta charset=""UTF-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>{safeTitle} - {safeSubtitle}</title>
</head>
<body style=""margin:0;padding:24px 12px;background:#0b0b0b;font-family:Segoe UI,Arial,sans-serif;color:#f0f0f0;"">
    <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"">
        <tr>
            <td align=""center"">
                <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"" style=""max-width:560px;background:#141414;border:1px solid #2a2a2a;border-radius:10px;overflow:hidden;"">
                    <tr>
                        <td style=""padding:22px 24px;border-bottom:1px solid #2a2a2a;"">
                            <div style=""font-size:22px;font-weight:700;letter-spacing:0.5px;color:#ffffff;"">{safeTitle}</div>
                            <div style=""margin-top:4px;font-size:12px;color:#d4af37;text-transform:uppercase;letter-spacing:1px;"">{safeSubtitle}</div>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding:22px 24px;font-size:14px;line-height:1.6;color:#d7d7d7;"">
                            <p style=""margin:0 0 12px 0;color:#ffffff;font-size:15px;"">{safeGreeting}</p>
                            {bodyHtml}
                            {actionBlock}
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding:14px 24px;border-top:1px solid #2a2a2a;font-size:11px;color:#9a9a9a;"">
                            Bloodwave Team
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }
}

/// <summary>
/// Validation result helper for registration input
/// </summary>
internal record RegistrationValidation(bool IsValid, string? ErrorMessage)
{
    public AuthResponseDto ToResponse() =>
        new() { Success = false, Message = ErrorMessage ?? "Validation failed" };
}
