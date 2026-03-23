using System.ComponentModel.DataAnnotations;

namespace Project_Bloodwave_Backend.DTOs;

public class ResetPasswordRequestDto
{
    [Required]
    [MinLength(16)]
    public string Token { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;
}
