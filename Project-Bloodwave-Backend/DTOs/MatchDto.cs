namespace Project_Bloodwave_Backend.DTOs;

public class MatchDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int Time { get; set; }
    public int Level { get; set; }
    public int MaxHealth { get; set; }
    public string? Weapon1 { get; set; }
    public string? Weapon2 { get; set; }
    public string? Weapon3 { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<int>? ItemIds { get; set; }
}
