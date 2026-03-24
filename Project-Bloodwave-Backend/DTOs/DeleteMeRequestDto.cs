using System.ComponentModel.DataAnnotations;

namespace Project_Bloodwave_Backend.DTOs;

public class DeleteMeRequestDto
{
    [Required]
    [MinLength(1)]
    public string Password { get; set; } = string.Empty;
}
