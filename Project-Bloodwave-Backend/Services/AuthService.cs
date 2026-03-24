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

        return $@"<!DOCTYPE html>
<html lang=""en"" xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <meta charset=""UTF-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>Reset Your Password - Bloodwave</title>
    <style type=""text/css"">
        body {{margin: 0; padding: 0; min-width: 100%!important; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Roboto', 'Oxygen', 'Ubuntu', sans-serif; font-size: 16px; line-height: 1.5;}}
        table {{border-collapse: collapse; border-spacing: 0;}}
        img {{max-width: 100%; height: auto; display: block;}}
        .container {{width: 100%; max-width: 600px; margin: 0 auto;}}
        .header {{text-align: center; padding: 30px 20px; background: linear-gradient(135deg, #1a1a1a 0%, #2a1a1a 100%); border-bottom: 3px solid #d4af37;}}
        .header h1 {{margin: 0; color: #ffffff; font-size: 32px; font-weight: 600; letter-spacing: 2px;}}
        .header p {{margin: 8px 0 0 0; color: #d4af37; font-size: 12px; letter-spacing: 1px; text-transform: uppercase;}}
        .body-content {{background: #0f0f0f; color: #e0e0e0; padding: 30px;}}
        .greeting {{font-size: 18px; color: #ffffff; margin: 0 0 20px 0; font-weight: 500;}}
        .message {{color: #cccccc; margin: 15px 0; line-height: 1.6;}}
        .button-wrapper {{text-align: center; margin: 30px 0;}}
        .button {{background: linear-gradient(135deg, #d4af37 0%, #c69c2a 100%); color: #000000; padding: 14px 36px; text-decoration: none; border-radius: 4px; font-weight: 600; font-size: 14px; letter-spacing: 1px; text-transform: uppercase; display: inline-block; box-shadow: 0 4px 15px rgba(212, 175, 55, 0.3);}}
        .warning {{background: #2a1a1a; padding: 15px; border-left: 3px solid #d4af37; margin: 20px 0; color: #e0e0e0; font-size: 13px;}}
        .expiry {{color: #d4af37; font-weight: 600;}}
        .footer {{background: #1a1a1a; color: #888888; font-size: 12px; padding: 20px; text-align: center; border-top: 1px solid #333333;}}
        .footer a {{color: #d4af37; text-decoration: none;}}
        hr {{border: none; border-top: 1px solid #333333; margin: 20px 0;}}
    </style>
</head>
<body>
    <table class=""container"" role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"">
        <tr><td class=""header"">
            <h1>⚔ BLOODWAVE</h1>
            <p>Password Recovery</p>
        </td></tr>
        <tr><td class=""body-content"">
            <p class=""greeting"">Hello {safeUsername},</p>
            <p class=""message"">We received a request to reset the password for your Bloodwave account. If this wasn't you, you can safely ignore this email.</p>
            <div class=""button-wrapper"">
                <a href=""{safeResetUrl}"" class=""button"">Reset Password</a>
            </div>
            <p class=""message"" style=""font-size: 13px; color: #999999;"">
                Or copy and paste this link into your browser:<br />
                <span style=""word-break: break-all; color: #d4af37;"">{safeResetUrl}</span>
            </p>
            <div class=""warning"">
                <strong>⏳ Expires in:</strong> <span class=""expiry"">{expiresMinutes} minutes</span>. Act quickly!
            </div>
            <hr />
            <p class=""message"" style=""font-size: 12px; color: #888888;"">
                For security reasons, never share this link with anyone. Bloodwave support will never ask for your password.
            </p>
        </td></tr>
        <tr><td class=""footer"">
            <p style=""margin: 0 0 8px 0;"">&copy; 2026 Bloodwave. All rights reserved.</p>
            <p style=""margin: 0;""><a href=""https://bloodwave.game"">Visit our website</a> | <a href=""https://bloodwave.game/support"">Support</a></p>
        </td></tr>
    </table>
</body>
</html>";
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

        return $@"<!DOCTYPE html>
<html lang=""en"" xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <meta charset=""UTF-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>Password Changed - Bloodwave</title>
    <style type=""text/css"">
        body {{margin: 0; padding: 0; min-width: 100%!important; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Roboto', sans-serif; font-size: 16px; line-height: 1.5;}}
        table {{border-collapse: collapse; border-spacing: 0;}}
        img {{max-width: 100%; height: auto; display: block;}}
        .container {{width: 100%; max-width: 600px; margin: 0 auto;}}
        .header {{text-align: center; padding: 30px 20px; background: linear-gradient(135deg, #1a1a1a 0%, #2a1a1a 100%); border-bottom: 3px solid #ff6b6b;}}
        .header h1 {{margin: 0; color: #ffffff; font-size: 32px; font-weight: 600; letter-spacing: 2px;}}
        .header p {{margin: 8px 0 0 0; color: #ff6b6b; font-size: 12px; letter-spacing: 1px; text-transform: uppercase;}}
        .body-content {{background: #0f0f0f; color: #e0e0e0; padding: 30px;}}
        .greeting {{font-size: 18px; color: #ffffff; margin: 0 0 20px 0; font-weight: 500;}}
        .message {{color: #cccccc; margin: 15px 0; line-height: 1.6;}}
        .success {{background: rgba(76, 175, 80, 0.1); border-left: 3px solid #4cb50; padding: 15px; margin: 20px 0; color: #90ee90; font-size: 13px;}}
        .alert {{background: #2a1a1a; border-left: 3px solid #ff6b6b; padding: 15px; margin: 20px 0; color: #e0e0e0; font-size: 13px;}}
        .alert strong {{color: #ff6b6b;}}
        .footer {{background: #1a1a1a; color: #888888; font-size: 12px; padding: 20px; text-align: center; border-top: 1px solid #333333;}}
        .footer a {{color: #d4af37; text-decoration: none;}}
        hr {{border: none; border-top: 1px solid #333333; margin: 20px 0;}}
    </style>
</head>
<body>
    <table class=""container"" role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"">
        <tr><td class=""header"">
            <h1>✔ BLOODWAVE</h1>
            <p>Security Update</p>
        </td></tr>
        <tr><td class=""body-content"">
            <p class=""greeting"">Hello {safeUsername},</p>
            <p class=""message"">Your Bloodwave account password has been successfully changed.</p>
            
            <div class=""success"">
                ✓ Your password has been updated successfully
            </div>
            
            <p class=""message"">If you did not make this change, your account may be compromised. Please:</p>
            <p class=""message"" style=""margin-left: 20px; color: #ff9999;"">
                • Immediately contact our <a href=""https://bloodwave.game/support"" style=""color: #d4af37; text-decoration: none;"">support team</a><br />
                • Change your password again<br />
                • Review your account activity
            </p>
            
            <hr />
            
            <div class=""alert"">
                🔒 <strong>Security Tip:</strong> Never share your password with anyone. Our team will never ask for it.
            </div>
            
            <p class=""message"" style=""font-size: 12px; color: #888888;"">
                If you have any questions or didn't authorize this change, please contact our support team immediately.
            </p>
        </td></tr>
        <tr><td class=""footer"">
            <p style=""margin: 0 0 8px 0;"">&copy; 2026 Bloodwave. All rights reserved.</p>
            <p style=""margin: 0;""><a href=""https://bloodwave.game"">Visit our website</a> | <a href=""https://bloodwave.game/support"">Support</a></p>
        </td></tr>
    </table>
</body>
</html>";
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

        return $@"<!DOCTYPE html>
<html lang=""en"" xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <meta charset=""UTF-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>Welcome to Bloodwave</title>
    <style type=""text/css"">
        body {{margin: 0; padding: 0; min-width: 100%!important; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Roboto', 'Oxygen', 'Ubuntu', sans-serif; font-size: 16px;}}
        table {{border-collapse: collapse; border-spacing: 0;}}
        img {{max-width: 100%; height: auto; display: block;}}
        .container {{width: 100%; max-width: 600px; margin: 0 auto;}}
        .header {{text-align: center; padding: 40px 20px; background: linear-gradient(135deg, #1a1a1a 0%, #2a1a1a 100%); border-bottom: 3px solid #d4af37;}}
        .header h1 {{margin: 0; color: #ffffff; font-size: 36px; font-weight: 700; letter-spacing: 2px;}}
        .header .tagline {{margin: 10px 0 0 0; color: #d4af37; font-size: 13px; letter-spacing: 2px; text-transform: uppercase;}}
        .hero {{text-align: center; padding: 40px 20px; background: linear-gradient(135deg, #0f0f0f 0%, #1a0f0f 100%);}}
        .hero .welcome {{font-size: 28px; color: #d4af37; margin: 0 0 10px 0; font-weight: 700;}}
        .hero .username {{font-size: 22px; color: #ffffff; margin: 0; font-weight: 600;}}
        .body-content {{background: #0f0f0f; color: #e0e0e0; padding: 30px;}}
        .section {{margin: 25px 0;}}
        .section-title {{color: #d4af37; font-size: 14px; font-weight: 700; text-transform: uppercase; letter-spacing: 1px; margin-bottom: 12px;}}
        .message {{color: #cccccc; margin: 12px 0; line-height: 1.6; font-size: 14px;}}
        .feature {{background: rgba(212, 175, 55, 0.05); border-left: 3px solid #d4af37; padding: 12px 15px; margin: 10px 0; color: #e0e0e0; font-size: 13px;}}
        .feature-list {{list-style: none; padding: 0; margin: 15px 0;}}
        .feature-list li {{padding: 8px 0; color: #cccccc; font-size: 13px;}}
        .feature-list li:before {{content: ""▸ ""; color: #d4af37; margin-right: 8px; font-weight: bold;}}
        .button-wrapper {{text-align: center; margin: 30px 0;}}
        .button {{background: linear-gradient(135deg, #d4af37 0%, #c69c2a 100%); color: #000000; padding: 12px 32px; text-decoration: none; border-radius: 4px; font-weight: 600; font-size: 13px; letter-spacing: 1px; text-transform: uppercase; display: inline-block; box-shadow: 0 4px 15px rgba(212, 175, 55, 0.3);}}
        .divider {{border: none; border-top: 1px solid #333333; margin: 25px 0;}}
        .footer {{background: #1a1a1a; color: #888888; font-size: 11px; padding: 20px; text-align: center; border-top: 1px solid #333333;}}
        .footer a {{color: #d4af37; text-decoration: none;}}
        .footer p {{margin: 5px 0;}}
        .social {{margin-top: 10px;}}
        .social a {{color: #d4af37; text-decoration: none; margin: 0 10px; font-size: 12px;}}
    </style>
</head>
<body>
    <table class=""container"" role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"">
        <tr><td class=""header"">
            <h1>⚔ BLOODWAVE ⚔</h1>
            <p class=""tagline"">Welcome to the Covenant</p>
        </td></tr>
        
        <tr><td class=""hero"">
            <div class=""welcome"">Welcome to the Arena</div>
            <div class=""username"">{safeUsername}</div>
        </td></tr>
        
        <tr><td class=""body-content"">
            <p class=""message"" style=""margin-top: 0;"">
                Your journey begins now. Your registration was successful, and your warrior is ready to enter the battlefield.
            </p>
            
            <div class=""section"">
                <div class=""section-title"">🗡 Ready to Fight?</div>
                <div class=""message"">
                    Log in to your account and prepare for battle. Sharpen your skills, collect powerful weapons, and prove your worth in the arena.
                </div>
                <ul class=""feature-list"">
                    <li>Customize your warrior</li>
                    <li>Master multiple weapons</li>
                    <li>Climb the leaderboards</li>
                    <li>Unlock achievements</li>
                    <li>Join the community</li>
                </ul>
            </div>
            
            <div class=""button-wrapper"">
                <a href=""https://bloodwave.game/login"" class=""button"">Start Your Journey</a>
            </div>
            
            <div class=""feature"">
                💡 <strong>Pro Tip:</strong> Check out our <a href=""https://bloodwave.game/guide"" style=""color: #d4af37; text-decoration: none;"">beginner's guide</a> to master the basics quickly.
            </div>
            
            <div class=""divider""></div>
            
            <div class=""section"">
                <div class=""section-title"">❓ Need Help?</div>
                <p class=""message"">Our support team is here for you. If you have any questions or need assistance, visit our <a href=""https://bloodwave.game/support"" style=""color: #d4af37; text-decoration: none;"">support center</a>.</p>
            </div>
            
            <p class=""message"" style=""font-size: 12px; color: #888888; margin-bottom: 0;"">
                Thank you for joining Bloodwave. We're honored to have you as part of our community!
            </p>
        </td></tr>
        
        <tr><td class=""footer"">
            <p style=""margin: 0 0 8px 0;"">&copy; 2026 Bloodwave. All rights reserved.</p>
            <p style=""margin: 0 0 8px 0;"">
                <a href=""https://bloodwave.game"">Visit Website</a> | 
                <a href=""https://bloodwave.game/support"">Support</a> | 
                <a href=""https://bloodwave.game/terms"">Terms</a>
            </p>
            <div class=""social"">
                <a href=""https://discord.gg/bloodwave"">Discord</a> | 
                <a href=""https://twitter.com/playbw"">Twitter</a>
            </div>
        </td></tr>
    </table>
</body>
</html>";
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

        return $@"<!DOCTYPE html>
<html lang=""en"" xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <meta charset=""UTF-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>Email Address Updated - Bloodwave</title>
    <style type=""text/css"">
        body {{margin: 0; padding: 0; min-width: 100%!important; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Roboto', sans-serif; font-size: 16px; line-height: 1.5;}}
        table {{border-collapse: collapse; border-spacing: 0;}}
        img {{max-width: 100%; height: auto; display: block;}}
        .container {{width: 100%; max-width: 600px; margin: 0 auto;}}
        .header {{text-align: center; padding: 30px 20px; background: linear-gradient(135deg, #1a1a1a 0%, #2a1a1a 100%); border-bottom: 3px solid #4da6ff;}}
        .header h1 {{margin: 0; color: #ffffff; font-size: 32px; font-weight: 600; letter-spacing: 2px;}}
        .header p {{margin: 8px 0 0 0; color: #4da6ff; font-size: 12px; letter-spacing: 1px; text-transform: uppercase;}}
        .body-content {{background: #0f0f0f; color: #e0e0e0; padding: 30px;}}
        .greeting {{font-size: 18px; color: #ffffff; margin: 0 0 20px 0; font-weight: 500;}}
        .message {{color: #cccccc; margin: 15px 0; line-height: 1.6;}}
        .info-box {{background: rgba(77, 166, 255, 0.08); border: 1px solid rgba(77, 166, 255, 0.2); border-left: 3px solid #4da6ff; padding: 15px; margin: 20px 0; color: #e0e0e0; font-size: 13px;}}
        .email-label {{color: #999999; font-size: 12px; margin-bottom: 3px;}}
        .email-value {{color: #4da6ff; font-weight: 600; font-size: 13px; font-family: 'Courier New', monospace;}}
        .alert {{background: #2a1a1a; border-left: 3px solid #ff6b6b; padding: 15px; margin: 20px 0; color: #e0e0e0; font-size: 13px;}}
        .alert strong {{color: #ff6b6b;}}
        .footer {{background: #1a1a1a; color: #888888; font-size: 12px; padding: 20px; text-align: center; border-top: 1px solid #333333;}}
        .footer a {{color: #4da6ff; text-decoration: none;}}
        hr {{border: none; border-top: 1px solid #333333; margin: 20px 0;}}
    </style>
</head>
<body>
    <table class=""container"" role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"">
        <tr><td class=""header"">
            <h1>✉ BLOODWAVE</h1>
            <p>Email Updated</p>
        </td></tr>
        <tr><td class=""body-content"">
            <p class=""greeting"">Hello {safeUsername},</p>
            <p class=""message"">Your Bloodwave account email address has been successfully updated.</p>
            
            <div class=""info-box"">
                <div class=""email-label"">Previous email:</div>
                <div class=""email-value"">{safeOldEmail}</div>
                <div style=""margin: 12px 0 0 0; text-align: center; color: #666666;"">↓</div>
                <div class=""email-label"" style=""margin-top: 12px;"">New email:</div>
                <div class=""email-value"">{safeNewEmail}</div>
            </div>
            
            <p class=""message"">From now on, use your new email address for logging in.</p>
            
            <hr />
            
            <div class=""alert"">
                🔔 <strong>Important:</strong> If you did not make this change, someone else may have access to your account. <a href=""https://bloodwave.game/support"" style=""color: #ff9999; text-decoration: none;"">Contact support immediately</a>.
            </div>
            
            <p class=""message"" style=""font-size: 12px; color: #888888;"">
                You've received this notification at both your old and new email addresses to confirm this change.
            </p>
        </td></tr>
        <tr><td class=""footer"">
            <p style=""margin: 0 0 8px 0;"">&copy; 2026 Bloodwave. All rights reserved.</p>
            <p style=""margin: 0;""><a href=""https://bloodwave.game"">Visit our website</a> | <a href=""https://bloodwave.game/support"">Support</a></p>
        </td></tr>
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
