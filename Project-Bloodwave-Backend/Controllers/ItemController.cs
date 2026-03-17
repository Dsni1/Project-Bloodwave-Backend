using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project_Bloodwave_Backend.DTOs;
using Project_Bloodwave_Backend.Services;

namespace Project_Bloodwave_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ItemController : ControllerBase
{
    private readonly IGameCrudService _crudService;

    public ItemController(IGameCrudService crudService)
    {
        _crudService = crudService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ItemDto>>> GetAll()
    {
        return Ok(await _crudService.GetItemsAsync());
    }

    [HttpGet("{itemId:int}")]
    public async Task<ActionResult<ItemDto>> GetById(int itemId)
    {
        var item = await _crudService.GetItemByIdAsync(itemId);
        return item == null ? NotFound(new { message = "Item not found" }) : Ok(item);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ItemDto>> Create([FromBody] ItemUpsertDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var created = await _crudService.CreateItemAsync(dto);
        return CreatedAtAction(nameof(GetById), new { itemId = created.Id }, created);
    }

    [HttpPut("{itemId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ItemDto>> Update(int itemId, [FromBody] ItemUpsertDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var updated = await _crudService.UpdateItemAsync(itemId, dto);
        return updated == null ? NotFound(new { message = "Item not found" }) : Ok(updated);
    }

    [HttpDelete("{itemId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(int itemId)
    {
        var deleted = await _crudService.DeleteItemAsync(itemId);
        return deleted ? Ok(new { success = true }) : NotFound(new { message = "Item not found" });
    }
}
