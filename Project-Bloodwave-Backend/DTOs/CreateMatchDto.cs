using System.Text.Json.Serialization;

namespace Project_Bloodwave_Backend.DTOs;

public class CreateMatchDto
{
    public int Time { get; set; }
    public int Level { get; set; }
    public int MaxHealth { get; set; }
    public int DamageDealt { get; set; }
    public int DamageTaken { get; set; }
    public int EnemiesKilled { get; set; }
    public int CoinsCollected { get; set; }
    public List<int>? ItemIds { get; set; } = new List<int>();
    [JsonPropertyName("weaponIds")]
    public List<int>? WeaponIds { get; set; } = new List<int>();

    [JsonPropertyName("weaponsIds")]
    public List<int>? WeaponsIds
    {
        get => WeaponIds;
        set => WeaponIds = value;
    }
}
