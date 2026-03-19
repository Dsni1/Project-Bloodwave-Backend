using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project_Bloodwave_Backend.DTOs;
using Project_Bloodwave_Backend.Extensions;
using Project_Bloodwave_Backend.Services;

namespace Project_Bloodwave_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MatchController : ControllerBase
{
    private readonly IGameCrudService _crudService;

    public MatchController(IGameCrudService crudService)
    {
        _crudService = crudService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<MatchDto>>> GetAll()
    {
        var validationError = this.ValidateAndGetUserId(out int userId);
        if (validationError != null)
            return validationError;

        return Ok(await _crudService.GetMatchesByUserAsync(userId));
    }

    [HttpGet("player")]
    [AllowAnonymous]
    public async Task<ActionResult<List<MatchDto>>> GetMatchesForPlayer([FromQuery] int playerId)
    {
        return Ok(await _crudService.GetMatchesByUserAsync(playerId));
    }

    [HttpGet("{matchId:int}")]
    public async Task<ActionResult<MatchDto>> GetById(int matchId)
    {
        var validationError = this.ValidateAndGetUserId(out int userId);
        if (validationError != null)
            return validationError;

        var match = await _crudService.GetMatchByIdAsync(userId, matchId);
        return match == null ? NotFound(new { message = "Match not found" }) : Ok(match);
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<MatchDto>> Create([FromBody] CreateMatchDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var validationError = this.ValidateAndGetUserId(out int userId);
        if (validationError != null)
            return validationError;

        var created = await _crudService.CreateMatchAsync(userId, dto);
        return CreatedAtAction(nameof(GetById), new { matchId = created.Id }, created);
    }

    [HttpPut("{matchId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<MatchDto>> Update(int matchId, [FromBody] UpdateMatchDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var validationError = this.ValidateAndGetUserId(out int userId);
        if (validationError != null)
            return validationError;

        var updated = await _crudService.UpdateMatchAsync(userId, matchId, dto);
        return updated == null ? NotFound(new { message = "Match not found" }) : Ok(updated);
    }

    [HttpDelete("{matchId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(int matchId)
    {
        var validationError = this.ValidateAndGetUserId(out int userId);
        if (validationError != null)
            return validationError;

        var deleted = await _crudService.DeleteMatchAsync(userId, matchId);
        return deleted ? Ok(new { success = true }) : NotFound(new { message = "Match not found" });
    }
}
