using System.ComponentModel.DataAnnotations;

namespace Project_Bloodwave_Backend.DTOs;

public class SendMailDto
{
    [Required]
    [EmailAddress]
    public string To { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public string Text { get; set; } = string.Empty;

    public string? Html { get; set; }
}
