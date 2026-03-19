using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project_Bloodwave_Backend.DTOs;
using Project_Bloodwave_Backend.Extensions;
using Project_Bloodwave_Backend.Services;

namespace Project_Bloodwave_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IGameCrudService _crudService;
    private readonly IAuthService _authService;

    public UserController(IGameCrudService crudService, IAuthService authService)
    {
        _crudService = crudService;
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult<AuthResponseDto>> Create([FromBody] RegisterDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.RegisterAsync(dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.LoginAsync(dto);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<ActionResult<AuthResponseDto>> Logout()
    {
        var validationError = this.ValidateAndGetUserId(out int userId);
        if (validationError != null)
            return validationError;

        var result = await _authService.LogoutAsync(userId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetMe()
    {
        var validationError = this.ValidateAndGetUserId(out int userId);
        if (validationError != null)
            return validationError;

        var user = await _crudService.GetUserByIdAsync(userId);
        return user == null ? NotFound(new { message = "User not found" }) : Ok(user);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("{userId:int}")]
    public async Task<ActionResult<UserDto>> GetById(int userId)
    {
        var user = await _crudService.GetUserByIdAsync(userId);
        return user == null ? NotFound(new { message = "User not found" }) : Ok(user);
    }

    [HttpPut("me")]
    public async Task<ActionResult<UserDto>> UpdateMe([FromBody] UpdateUserDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var validationError = this.ValidateAndGetUserId(out int userId);
        if (validationError != null)
            return validationError;

        var user = await _crudService.UpdateUserAsync(userId, dto);
        return user == null ? NotFound(new { message = "User not found" }) : Ok(user);
    }

    [HttpPut("{userId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserDto>> Update(int userId, [FromBody] UpdateUserDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _crudService.UpdateUserAsync(userId, dto);
        return user == null ? NotFound(new { message = "User not found" }) : Ok(user);
    }

    [HttpDelete("me")]
    public async Task<ActionResult> DeleteMe()
    {
        var validationError = this.ValidateAndGetUserId(out int userId);
        if (validationError != null)
            return validationError;

        var deleted = await _crudService.DeleteUserAsync(userId);
        return deleted ? Ok(new { success = true }) : NotFound(new { message = "User not found" });
    }

    [HttpDelete("{userId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(int userId)
    {
        var deleted = await _crudService.DeleteUserAsync(userId);
        return deleted ? Ok(new { success = true }) : NotFound(new { message = "User not found" });
    }
}
