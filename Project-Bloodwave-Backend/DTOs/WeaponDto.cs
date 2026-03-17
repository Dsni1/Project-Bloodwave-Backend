namespace Project_Bloodwave_Backend.DTOs;

public class WeaponDto
{
    public int Id { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class WeaponUpsertDto
{
    public string ItemName { get; set; } = string.Empty;
    public string? Description { get; set; }
}
