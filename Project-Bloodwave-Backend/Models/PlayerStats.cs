using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project_Bloodwave_Backend.Models;

[Table("PlayerStats")]
public class PlayerStats
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    [InverseProperty(nameof(User.PlayerStats))]
    public User? User { get; set; }

    [Column("total_kills")]
    public int TotalKills { get; set; } = 0;

    [Column("highest_level")]
    public int HighestLevel { get; set; } = 1;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
