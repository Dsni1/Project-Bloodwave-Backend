using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Project_Bloodwave_Backend.DTOs;
using Project_Bloodwave_Backend.Services;
using System.Security.Claims;

namespace Project_Bloodwave_Backend.Controllers;

/// <summary>
/// Player stats and match endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PlayerController : ControllerBase
{
    private readonly IPlayerService _playerService;

    public PlayerController(IPlayerService playerService)
    {
        _playerService = playerService;
    }

    /// <summary>
    /// Get current player's stats
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<PlayerStatsDto>> GetStats()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            return Unauthorized(new { message = "Invalid token" });

        var stats = await _playerService.GetPlayerStatsAsync(userId);
        if (stats == null)
            return NotFound(new { message = "Player stats not found" });

        return Ok(stats);
    }

    /// <summary>
    /// Create a new match for current player
    /// </summary>
    [HttpPost("match")]
    public async Task<ActionResult<MatchDto>> CreateMatch([FromBody] CreateMatchDto createMatchDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            return Unauthorized(new { message = "Invalid token" });

        var match = await _playerService.CreateMatchAsync(userId, createMatchDto);
        return CreatedAtAction(nameof(CreateMatch), match);
    }

    /// <summary>
    /// Get all matches for current player
    /// </summary>
    [HttpGet("matches")]
    public async Task<ActionResult<List<MatchDto>>> GetAllMatches()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            return Unauthorized(new { message = "Invalid token" });

        var matches = await _playerService.GetAllMatchesAsync(userId);
        return Ok(matches);
    }

    /// <summary>
    /// Get a specific match by ID
    /// </summary>
    [HttpGet("match/{matchId}")]
    public async Task<ActionResult<MatchDto>> GetMatch(int matchId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            return Unauthorized(new { message = "Invalid token" });

        var match = await _playerService.GetMatchByIdAsync(matchId, userId);
        if (match == null)
            return NotFound(new { message = "Match not found" });

        return Ok(match);
    }
}
