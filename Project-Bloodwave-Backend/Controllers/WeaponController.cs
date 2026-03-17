using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project_Bloodwave_Backend.DTOs;
using Project_Bloodwave_Backend.Services;

namespace Project_Bloodwave_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WeaponController : ControllerBase
{
    private readonly IGameCrudService _crudService;

    public WeaponController(IGameCrudService crudService)
    {
        _crudService = crudService;
    }

    [HttpGet]
    public async Task<ActionResult<List<WeaponDto>>> GetAll()
    {
        return Ok(await _crudService.GetWeaponsAsync());
    }

    [HttpGet("{weaponId:int}")]
    public async Task<ActionResult<WeaponDto>> GetById(int weaponId)
    {
        var weapon = await _crudService.GetWeaponByIdAsync(weaponId);
        return weapon == null ? NotFound(new { message = "Weapon not found" }) : Ok(weapon);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<WeaponDto>> Create([FromBody] WeaponUpsertDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var created = await _crudService.CreateWeaponAsync(dto);
        return CreatedAtAction(nameof(GetById), new { weaponId = created.Id }, created);
    }

    [HttpPut("{weaponId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<WeaponDto>> Update(int weaponId, [FromBody] WeaponUpsertDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var updated = await _crudService.UpdateWeaponAsync(weaponId, dto);
        return updated == null ? NotFound(new { message = "Weapon not found" }) : Ok(updated);
    }

    [HttpDelete("{weaponId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(int weaponId)
    {
        var deleted = await _crudService.DeleteWeaponAsync(weaponId);
        return deleted ? Ok(new { success = true }) : NotFound(new { message = "Weapon not found" });
    }
}
