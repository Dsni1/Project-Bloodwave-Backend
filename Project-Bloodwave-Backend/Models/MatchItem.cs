using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project_Bloodwave_Backend.Models;

[Table("MatchItems")]
public class MatchItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("match_id")]
    public int MatchId { get; set; }

    [ForeignKey(nameof(MatchId))]
    [InverseProperty(nameof(Match.MatchItems))]
    public Match? Match { get; set; }

    [Required]
    [Column("item_id")]
    public int ItemId { get; set; }

    [ForeignKey(nameof(ItemId))]
    [InverseProperty(nameof(Item.MatchItems))]
    public Item? Item { get; set; }
}
