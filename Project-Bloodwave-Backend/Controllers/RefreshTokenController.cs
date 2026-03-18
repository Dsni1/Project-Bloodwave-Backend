using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_Bloodwave_Backend.Data;
using Project_Bloodwave_Backend.DTOs;
using Project_Bloodwave_Backend.Extensions;
using Project_Bloodwave_Backend.Models;
using System.Security.Cryptography;

namespace Project_Bloodwave_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RefreshTokenController : ControllerBase
{
    private readonly BloodwaveDbContext _context;

    public RefreshTokenController(BloodwaveDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<RefreshTokenDto>>> GetAll()
    {
        var validationError = this.ValidateAndGetUserId(out int userId);
        if (validationError != null)
            return validationError;

        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId)
            .OrderByDescending(rt => rt.CreatedAt)
            .Select(rt => new RefreshTokenDto
            {
                RefreshToken = rt.Token,
                ExpiresAt = rt.ExpiresAt
            })
            .ToListAsync();

        return Ok(tokens);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RefreshTokenDto>> GetById(int id)
    {
        var validationError = this.ValidateAndGetUserId(out int userId);
        if (validationError != null)
            return validationError;

        var token = await _context.RefreshTokens
            .Where(rt => rt.Id == id && rt.UserId == userId)
            .Select(rt => new RefreshTokenDto
            {
                RefreshToken = rt.Token,
                ExpiresAt = rt.ExpiresAt
            })
            .FirstOrDefaultAsync();

        return token == null ? NotFound(new { message = "Refresh token not found" }) : Ok(token);
    }

    [HttpPost]
    public async Task<ActionResult<RefreshTokenDto>> Create([FromBody] RefreshTokenDto dto)
    {
        var validationError = this.ValidateAndGetUserId(out int userId);
        if (validationError != null)
            return validationError;

        var token = new RefreshToken
        {
            UserId = userId,
            Token = GenerateToken(),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = dto.ExpiresAt == default ? DateTime.UtcNow.AddDays(7) : dto.ExpiresAt,
            CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        _context.RefreshTokens.Add(token);
        await _context.SaveChangesAsync();

        var response = new RefreshTokenDto
        {
            RefreshToken = token.Token,
            ExpiresAt = token.ExpiresAt
        };

        return CreatedAtAction(nameof(GetById), new { id = token.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<RefreshTokenDto>> Update(int id, [FromBody] RefreshTokenDto dto)
    {
        var validationError = this.ValidateAndGetUserId(out int userId);
        if (validationError != null)
            return validationError;

        if (string.IsNullOrWhiteSpace(dto.RefreshToken))
            return BadRequest(new { message = "Refresh token is required" });

        var oldToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Id == id && rt.UserId == userId);

        if (oldToken == null)
            return NotFound(new { message = "Refresh token not found" });

        if (!string.Equals(oldToken.Token, dto.RefreshToken, StringComparison.Ordinal))
            return BadRequest(new { message = "Provided refresh token does not match the requested token" });

        if (!oldToken.IsActive)
            return BadRequest(new { message = "Refresh token is already revoked or expired" });

        oldToken.RevokedAt = DateTime.UtcNow;

        var newToken = new RefreshToken
        {
            UserId = userId,
            Token = GenerateToken(),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = dto.ExpiresAt == default ? DateTime.UtcNow.AddDays(7) : dto.ExpiresAt,
            ReplacesToken = oldToken.Token,
            CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString()
        };

        _context.RefreshTokens.Add(newToken);

        await _context.SaveChangesAsync();

        var response = new RefreshTokenDto
        {
            RefreshToken = newToken.Token,
            ExpiresAt = newToken.ExpiresAt
        };

        return CreatedAtAction(nameof(GetById), new { id = newToken.Id }, response);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(int id)
    {
        var validationError = this.ValidateAndGetUserId(out int userId);
        if (validationError != null)
            return validationError;

        var token = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Id == id && rt.UserId == userId);
        if (token == null)
            return NotFound(new { message = "Refresh token not found" });

        _context.RefreshTokens.Remove(token);
        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }

    private static string GenerateToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
