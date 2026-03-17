using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project_Bloodwave_Backend.DTOs;
using Project_Bloodwave_Backend.Extensions;
using Project_Bloodwave_Backend.Services;

namespace Project_Bloodwave_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CrudController : ControllerBase
{
    private readonly IGameCrudService _crudService;

    public CrudController(IGameCrudService crudService)
    {
        _crudService = crudService;
    }

    [HttpGet("items")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<ItemDto>>> GetItems()
    {
        return Ok(await _crudService.GetItemsAsync());
    }

    [HttpGet("items/{itemId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ItemDto>> GetItem(int itemId)
    {
        var item = await _crudService.GetItemByIdAsync(itemId);
        return item == null ? NotFound(new { message = "Item not found" }) : Ok(item);
    }

    [HttpPost("items")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ItemDto>> CreateItem([FromBody] ItemUpsertDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var created = await _crudService.CreateItemAsync(dto);
        return CreatedAtAction(nameof(GetItem), new { itemId = created.Id }, created);
    }

    [HttpPut("items/{itemId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ItemDto>> UpdateItem(int itemId, [FromBody] ItemUpsertDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var updated = await _crudService.UpdateItemAsync(itemId, dto);
        return updated == null ? NotFound(new { message = "Item not found" }) : Ok(updated);
    }

    [HttpDelete("items/{itemId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeleteItem(int itemId)
    {
        var deleted = await _crudService.DeleteItemAsync(itemId);
        return deleted ? Ok(new { success = true }) : NotFound(new { message = "Item not found" });
    }

    [HttpGet("weapons")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<WeaponDto>>> GetWeapons()
    {
        return Ok(await _crudService.GetWeaponsAsync());
    }

    [HttpGet("weapons/{weaponId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<WeaponDto>> GetWeapon(int weaponId)
    {
        var weapon = await _crudService.GetWeaponByIdAsync(weaponId);
        return weapon == null ? NotFound(new { message = "Weapon not found" }) : Ok(weapon);
    }

    [HttpPost("weapons")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<WeaponDto>> CreateWeapon([FromBody] WeaponUpsertDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var created = await _crudService.CreateWeaponAsync(dto);
        return CreatedAtAction(nameof(GetWeapon), new { weaponId = created.Id }, created);
    }

    [HttpPut("weapons/{weaponId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<WeaponDto>> UpdateWeapon(int weaponId, [FromBody] WeaponUpsertDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var updated = await _crudService.UpdateWeaponAsync(weaponId, dto);
        return updated == null ? NotFound(new { message = "Weapon not found" }) : Ok(updated);
    }

    [HttpDelete("weapons/{weaponId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeleteWeapon(int weaponId)
    {
        var deleted = await _crudService.DeleteWeaponAsync(weaponId);
        return deleted ? Ok(new { success = true }) : NotFound(new { message = "Weapon not found" });
    }

    [HttpGet("matches")]
    public async Task<ActionResult<List<MatchDto>>> GetMatches()
    {
        var validationError = this.ValidateAndGetUserId(out int userId);
        if (validationError != null)
            return validationError;

        return Ok(await _crudService.GetMatchesByUserAsync(userId));
    }

    [HttpGet("matches/{matchId:int}")]
    public async Task<ActionResult<MatchDto>> GetMatch(int matchId)
    {
        var validationError = this.ValidateAndGetUserId(out int userId);
        if (validationError != null)
            return validationError;

        var match = await _crudService.GetMatchByIdAsync(userId, matchId);
        return match == null ? NotFound(new { message = "Match not found" }) : Ok(match);
    }

    [HttpPost("matches")]
    public async Task<ActionResult<MatchDto>> CreateMatch([FromBody] CreateMatchDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var validationError = this.ValidateAndGetUserId(out int userId);
        if (validationError != null)
            return validationError;

        var created = await _crudService.CreateMatchAsync(userId, dto);
        return CreatedAtAction(nameof(GetMatch), new { matchId = created.Id }, created);
    }

    [HttpPut("matches/{matchId:int}")]
    public async Task<ActionResult<MatchDto>> UpdateMatch(int matchId, [FromBody] UpdateMatchDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var validationError = this.ValidateAndGetUserId(out int userId);
        if (validationError != null)
            return validationError;

        var updated = await _crudService.UpdateMatchAsync(userId, matchId, dto);
        return updated == null ? NotFound(new { message = "Match not found" }) : Ok(updated);
    }

    [HttpDelete("matches/{matchId:int}")]
    public async Task<ActionResult> DeleteMatch(int matchId)
    {
        var validationError = this.ValidateAndGetUserId(out int userId);
        if (validationError != null)
            return validationError;

        var deleted = await _crudService.DeleteMatchAsync(userId, matchId);
        return deleted ? Ok(new { success = true }) : NotFound(new { message = "Match not found" });
    }

    [HttpGet("profile")]
    public async Task<ActionResult<UserDto>> GetProfile()
    {
        var validationError = this.ValidateAndGetUserId(out int userId);
        if (validationError != null)
            return validationError;

        var user = await _crudService.GetUserByIdAsync(userId);
        return user == null ? NotFound(new { message = "User not found" }) : Ok(user);
    }

    [HttpPut("profile")]
    public async Task<ActionResult<UserDto>> UpdateProfile([FromBody] UpdateUserDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var validationError = this.ValidateAndGetUserId(out int userId);
        if (validationError != null)
            return validationError;

        var user = await _crudService.UpdateUserAsync(userId, dto);
        return user == null ? NotFound(new { message = "User not found" }) : Ok(user);
    }

    [HttpDelete("profile")]
    public async Task<ActionResult> DeleteProfile()
    {
        var validationError = this.ValidateAndGetUserId(out int userId);
        if (validationError != null)
            return validationError;

        var deleted = await _crudService.DeleteUserAsync(userId);
        return deleted ? Ok(new { success = true }) : NotFound(new { message = "User not found" });
    }
}
