namespace Project_Bloodwave_Backend.DTOs;

public class UpdateMatchDto
{
    public int Time { get; set; }
    public int Level { get; set; }
    public int MaxHealth { get; set; }
    public List<int>? ItemIds { get; set; } = new();
    public List<int>? WeaponIds { get; set; } = new();
}
