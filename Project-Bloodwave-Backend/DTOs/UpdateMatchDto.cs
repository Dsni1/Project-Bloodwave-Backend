using System.Text.Json.Serialization;

namespace Project_Bloodwave_Backend.DTOs;

public class UpdateMatchDto
{
    public int Time { get; set; }
    public int Level { get; set; }
    public int MaxHealth { get; set; }
    public List<int>? ItemIds { get; set; } = new();
    [JsonPropertyName("weaponIds")]
    public List<int>? WeaponIds { get; set; } = new();

    [JsonPropertyName("weaponsIds")]
    public List<int>? WeaponsIds
    {
        get => WeaponIds;
        set => WeaponIds = value;
    }
}
