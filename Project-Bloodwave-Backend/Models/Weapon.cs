using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project_Bloodwave_Backend.Models;

[Table("Weapons")]
public class Weapon
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("item_name")]
    public string ItemName { get; set; } = string.Empty;

    [MaxLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    // Navigation properties
    public ICollection<MatchWeapon> MatchWeapons { get; set; } = new List<MatchWeapon>();
}

