namespace Project_Bloodwave_Backend.DTOs;

public class UserAchievmentDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int AchievmentId { get; set; }
    public DateTime UnlockedAt { get; set; }
}
