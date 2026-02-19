using Project_Bloodwave_Backend.Data;
using Project_Bloodwave_Backend.DTOs;
using Project_Bloodwave_Backend.Models;
using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Project_Bloodwave_Backend.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
    Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
    Task<AuthResponseDto> LogoutAsync(int userId);
}

/// <summary>
/// Service responsible for user authentication and JWT token generation
/// </summary>
public class AuthService : IAuthService
{
    private readonly BloodwaveDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher;
    private readonly IConfiguration _configuration;
    
    private const string InvalidCredentialsMessage = "Invalid username or password";
    private const string DefaultJwtKey = "your-super-secret-key-that-must-be-at-least-32-characters-long-for-hmacsha256";
    private const string DefaultJwtIssuer = "BloodwaveApi";
    private const string DefaultJwtAudience = "BloodwaveClient";
    private const int TokenExpirationHours = 24;
    private const int RefreshTokenExpirationDays = 7;
    private const int RefreshTokenByteLength = 64;

    public AuthService(BloodwaveDbContext context, IConfiguration configuration)
    {
        _context = context;
        _passwordHasher = new PasswordHasher<User>();
        _configuration = configuration;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
    {
        var validationResult = ValidateInputAsync(registerDto);
        if (!validationResult.IsValid)
            return validationResult.ToResponse();

        var existingUserResult = CheckExistingUser(registerDto.Username, registerDto.Email);
        if (existingUserResult != null)
            return existingUserResult;

        var user = CreateUser(registerDto);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        await CreateRefreshTokenAsync(user.Id);

        return new AuthResponseDto
        {
            Success = true,
            Message = "User registered successfully",
            Token = GenerateJwtToken(user),
            User = MapToUserDto(user)
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
    {
        var user = _context.Users.FirstOrDefault(u => u.Username == loginDto.Username);
        if (user == null)
            return new AuthResponseDto { Success = false, Message = InvalidCredentialsMessage };

        var passwordVerificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, loginDto.Password);
        if (passwordVerificationResult == PasswordVerificationResult.Failed)
            return new AuthResponseDto { Success = false, Message = InvalidCredentialsMessage };

        if (!user.IsActive)
            return new AuthResponseDto { Success = false, Message = "User account is inactive" };

        await CreateRefreshTokenAsync(user.Id);

        return new AuthResponseDto
        {
            Success = true,
            Message = "Login successful",
            Token = GenerateJwtToken(user),
            User = MapToUserDto(user)
        };
    }

    public async Task<AuthResponseDto> LogoutAsync(int userId)
    {
        var refreshTokens = _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToList();

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

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email)
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
    private async Task CreateRefreshTokenAsync(int userId)
    {
        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = GenerateRandomToken(),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpirationDays),
            CreatedByIp = "127.0.0.1"
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Generates a cryptographically secure random token
    /// </summary>
    private string GenerateRandomToken()
    {
        var randomBytes = new byte[RefreshTokenByteLength];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
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
            PasswordHash = _passwordHasher.HashPassword(null!, registerDto.Password),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Checks if user already exists by username or email
    /// </summary>
    private AuthResponseDto? CheckExistingUser(string username, string email)
    {
        if (_context.Users.Any(u => u.Username == username))
            return new AuthResponseDto { Success = false, Message = "Username already exists" };

        if (_context.Users.Any(u => u.Email == email))
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
            Email = user.Email
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
    /// Validates registration input data
    /// </summary>
    private static RegistrationValidation ValidateInputAsync(RegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
            return new RegistrationValidation(false, "Username and password required");

        return new RegistrationValidation(true, null);
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
