using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project_Bloodwave_Backend.Models;

[Table("MatchWeapons")]
public class MatchWeapon
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("match_id")]
    public int MatchId { get; set; }

    [ForeignKey(nameof(MatchId))]
    [InverseProperty(nameof(Match.MatchWeapons))]
    public Match? Match { get; set; }

    [Required]
    [Column("weapon_id")]
    public int WeaponId { get; set; }

    [ForeignKey(nameof(WeaponId))]
    [InverseProperty(nameof(Weapon.MatchWeapons))]
    public Weapon? Weapon { get; set; }
}
