using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project_Bloodwave_Backend.Models;

[Table("UserAchievments")]
public class UserAchievment
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    [InverseProperty(nameof(User.UserAchievments))]
    public User? User { get; set; }

    [Required]
    [Column("achievment_id")]
    public int AchievmentId { get; set; }

    [ForeignKey(nameof(AchievmentId))]
    [InverseProperty(nameof(Achievment.UserAchievments))]
    public Achievment? Achievment { get; set; }

    [Required]
    [Column("unlocked_at")]
    public DateTime UnlockedAt { get; set; }
}