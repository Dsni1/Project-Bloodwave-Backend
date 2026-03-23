using System.ComponentModel.DataAnnotations;

namespace Project_Bloodwave_Backend.DTOs;

public class ForgotPasswordRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
