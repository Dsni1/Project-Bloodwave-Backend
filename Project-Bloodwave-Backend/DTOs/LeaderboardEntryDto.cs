namespace Project_Bloodwave_Backend.DTOs;

/// <summary>
/// Represents a single entry in the player leaderboard
/// </summary>
public class LeaderboardEntryDto
{
    public int Rank { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int TotalKills { get; set; }
    public int HighestLevel { get; set; }
    public DateTime UpdatedAt { get; set; }
}
