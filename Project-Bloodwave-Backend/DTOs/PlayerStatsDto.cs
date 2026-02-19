namespace Project_Bloodwave_Backend.DTOs;

public class PlayerStatsDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int TotalKills { get; set; }
    public int HighestLevel { get; set; }
    public DateTime UpdatedAt { get; set; }
}
