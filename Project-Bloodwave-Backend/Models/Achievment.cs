using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project_Bloodwave_Backend.Models;

[Table("Achievments")]
public class Achievment
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("title")]
    public string Title { get; set; } = null!;

    [Required]
    [MaxLength(1000)]
    [Column("description")]
    public string Description { get; set; } = null!;

    [InverseProperty(nameof(UserAchievment.Achievment))]
    public ICollection<UserAchievment> UserAchievments { get; set; } = new List<UserAchievment>();
}
