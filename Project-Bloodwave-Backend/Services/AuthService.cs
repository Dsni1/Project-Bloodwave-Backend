using Project_Bloodwave_Backend.Data;
using Project_Bloodwave_Backend.DTOs;
using Project_Bloodwave_Backend.Models;
using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Project_Bloodwave_Backend.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
    Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
    Task<AuthResponseDto> LogoutAsync(int userId);
}

public class AuthService : IAuthService
{
    private readonly BloodwaveDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher;
    private readonly IConfiguration _configuration;

    public AuthService(BloodwaveDbContext context, IConfiguration configuration)
    {
        _context = context;
        _passwordHasher = new PasswordHasher<User>();
        _configuration = configuration;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
    {
        if (string.IsNullOrWhiteSpace(registerDto.Username) || string.IsNullOrWhiteSpace(registerDto.Password))
            return new AuthResponseDto { Success = false, Message = "Username and password required" };

        var userExists = _context.Users.FirstOrDefault(u => u.Username == registerDto.Username);
        if (userExists != null)
            return new AuthResponseDto { Success = false, Message = "Username already exists" };

        var emailExists = _context.Users.FirstOrDefault(u => u.Email == registerDto.Email);
        if (emailExists != null)
            return new AuthResponseDto { Success = false, Message = "Email already registered" };

        var user = new User
        {
            Username = registerDto.Username,
            Email = registerDto.Email,
            PasswordHash = _passwordHasher.HashPassword(null!, registerDto.Password),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = GenerateJwtToken(user);
        var refreshToken = GenerateRefreshToken(user.Id);
        
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return new AuthResponseDto
        {
            Success = true,
            Message = "User registered successfully",
            Token = token,
            User = new UserDto { Id = user.Id, Username = user.Username, Email = user.Email }
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
    {
        var user = _context.Users.FirstOrDefault(u => u.Username == loginDto.Username);
        if (user == null)
            return new AuthResponseDto { Success = false, Message = "Invalid username or password" };

        var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, loginDto.Password);
        if (verifyResult == PasswordVerificationResult.Failed)
            return new AuthResponseDto { Success = false, Message = "Invalid username or password" };

        if (!user.IsActive)
            return new AuthResponseDto { Success = false, Message = "User account is inactive" };

        var token = GenerateJwtToken(user);
        var refreshToken = GenerateRefreshToken(user.Id);
        
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return new AuthResponseDto
        {
            Success = true,
            Message = "Login successful",
            Token = token,
            User = new UserDto { Id = user.Id, Username = user.Username, Email = user.Email }
        };
    }

    public async Task<AuthResponseDto> LogoutAsync(int userId)
    {
        var refreshTokens = _context.RefreshTokens.Where(rt => rt.UserId == userId && rt.RevokedAt == null);
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

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "your-super-secret-key-that-must-be-at-least-32-characters-long-for-hmacsha256")
        );
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "BloodwaveApi",
            audience: _configuration["Jwt:Audience"] ?? "BloodwaveClient",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private RefreshToken GenerateRefreshToken(int userId)
    {
        var randomBytes = new byte[64];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        return new RefreshToken
        {
            UserId = userId,
            Token = Convert.ToBase64String(randomBytes),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = "127.0.0.1"
        };
    }
}
