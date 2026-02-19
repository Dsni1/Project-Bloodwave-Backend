namespace Project_Bloodwave_Backend.DTOs;

public class CreateMatchDto
{
    public int Time { get; set; }
    public int Level { get; set; }
    public int MaxHealth { get; set; }
    public string? Weapon1 { get; set; }
    public string? Weapon2 { get; set; }
    public string? Weapon3 { get; set; }
    public List<int>? ItemIds { get; set; } = new List<int>();
}
