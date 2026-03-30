using System.ComponentModel.DataAnnotations;

namespace Project_Bloodwave_Backend.DTOs;

public class RefreshRequestDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}