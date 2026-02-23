namespace Project_Bloodwave_Backend.DTOs
{
    public class RefreshTokenDto
    {
        public string? RefreshToken { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}