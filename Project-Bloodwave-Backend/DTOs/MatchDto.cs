namespace Project_Bloodwave_Backend.DTOs;

public class MatchDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int Time { get; set; }
    public int Level { get; set; }
    public int MaxHealth { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<int>? ItemIds { get; set; }
    public List<int>? WeaponIds { get; set; }
    public List<MatchItemDto> MatchItems { get; set; } = new();
    public List<MatchWeaponDto> MatchWeapons { get; set; } = new();
}


