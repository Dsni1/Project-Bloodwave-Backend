using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project_Bloodwave_Backend.DTOs;
using Project_Bloodwave_Backend.Extensions;
using Project_Bloodwave_Backend.Services;

namespace Project_Bloodwave_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AchievmentController : ControllerBase
{
    private readonly IGameCrudService _crudService;

    public AchievmentController(IGameCrudService crudService)
    {
        _crudService = crudService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<AchievmentDto>>> GetAll()
    {
        return Ok(await _crudService.GetAchievmentsAsync());
    }

    [HttpGet("{achievmentId:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<AchievmentDto>> GetById(int achievmentId)
    {
        var achievment = await _crudService.GetAchievmentByIdAsync(achievmentId);
        return achievment == null ? NotFound(new { message = "Achievment not found" }) : Ok(achievment);
    }

    [HttpGet("me")]
    public async Task<ActionResult<List<UserAchievmentDto>>> GetMine()
    {
        var validationError = this.ValidateAndGetUserId(out int userId);
        if (validationError != null)
            return validationError;

        return Ok(await _crudService.GetUserAchievmentsAsync(userId));
    }

    [HttpGet("user/{userId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<UserAchievmentDto>>> GetByUserId(int userId)
    {
        return Ok(await _crudService.GetUserAchievmentsAsync(userId));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AchievmentDto>> Create([FromBody] AchievmentDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var created = await _crudService.CreateAchievmentAsync(dto);
        return CreatedAtAction(nameof(GetById), new { achievmentId = created.Id }, created);
    }

    [HttpPut("{achievmentId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AchievmentDto>> Update(int achievmentId, [FromBody] AchievmentDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var updated = await _crudService.UpdateAchievmentAsync(achievmentId, dto);
        return updated == null ? NotFound(new { message = "Achievment not found" }) : Ok(updated);
    }

    [HttpDelete("{achievmentId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(int achievmentId)
    {
        var deleted = await _crudService.DeleteAchievmentAsync(achievmentId);
        return deleted ? NoContent() : NotFound(new { message = "Achievment not found" });
    }

    [HttpPost("{achievmentId:int}/unlock")]
    public async Task<ActionResult<UserAchievmentDto>> Unlock(int achievmentId)
    {
        var validationError = this.ValidateAndGetUserId(out int userId);
        if (validationError != null)
            return validationError;

        var unlocked = await _crudService.UnlockAchievmentAsync(userId, achievmentId);
        return unlocked == null
            ? NotFound(new { message = "Achievment not found" })
            : Ok(unlocked);
    }
}
