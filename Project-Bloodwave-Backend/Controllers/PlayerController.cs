using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Project_Bloodwave_Backend.DTOs;
using Project_Bloodwave_Backend.Services;
using Project_Bloodwave_Backend.Extensions;

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

    public PlayerController(IPlayerService playerService) => _playerService = playerService;

    /// <summary>
    /// Get current player's stats
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<PlayerStatsDto>> GetStats()
    {
        var validationError = this.ValidateAndGetUserId(out int userId);
        if (validationError != null)
            return validationError;

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

        var validationError = this.ValidateAndGetUserId(out int userId);
        if (validationError != null)
            return validationError;

        var match = await _playerService.CreateMatchAsync(userId, createMatchDto);
        return CreatedAtAction(nameof(GetMatch), new { matchId = match.Id }, match);
    }

    /// <summary>
    /// Get all matches for current player
    /// </summary>
    [HttpGet("matches")]
    public async Task<ActionResult<List<MatchDto>>> GetAllMatches()
    {
        var validationError = this.ValidateAndGetUserId(out int userId);
        if (validationError != null)
            return validationError;

        var matches = await _playerService.GetAllMatchesAsync(userId);
        return Ok(matches);
    }

    /// <summary>
    /// Get a specific match by ID
    /// </summary>
    [HttpGet("match/{matchId}")]
    public async Task<ActionResult<MatchDto>> GetMatch(int matchId)
    {
        var validationError = this.ValidateAndGetUserId(out int userId);
        if (validationError != null)
            return validationError;

        var match = await _playerService.GetMatchByIdAsync(matchId, userId);
        if (match == null)
            return NotFound(new { message = "Match not found" });

        return Ok(match);
    }

    /// <summary>
    /// Get the global leaderboard of all players
    /// </summary>
    [HttpGet("leaderboard")]
    [AllowAnonymous]
    public async Task<ActionResult<List<LeaderboardEntryDto>>> GetLeaderboard([FromQuery] int limit = 100)
    {
        if (limit <= 0 || limit > 1000)
            limit = 100;

        var leaderboard = await _playerService.GetLeaderboardAsync(limit);
        return Ok(leaderboard);
    }
}
