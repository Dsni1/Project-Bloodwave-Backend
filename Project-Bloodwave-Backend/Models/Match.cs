using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project_Bloodwave_Backend.Models;

[Table("Matches")]
public class Match
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    [InverseProperty(nameof(User.Matches))]
    public User? User { get; set; }

    [Required]
    [Column("time")]
    public int Time { get; set; } // másodpercben

    [Required]
    [Column("level")]
    public int Level { get; set; }

    [Column("damage_dealt")]
    public int DamageDealt { get; set; }
    [Column("damage_taken")]
    public int DamageTaken { get; set; }

    [Column("enemies_killed")]
    public int EnemiesKilled { get; set; }
    [Column("coins_collected")]
    public int CoinsCollected { get; set; }
    [Required]
    [Column("max_health")]
    public int MaxHealth { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [InverseProperty(nameof(MatchItem.Match))]
    public ICollection<MatchItem> MatchItems { get; set; } = new List<MatchItem>();

    [InverseProperty(nameof(MatchWeapon.Match))]
    public ICollection<MatchWeapon> MatchWeapons { get; set; } = new List<MatchWeapon>();
}
