namespace VisionAid.Api.DTOs.Admins;

public class AdminAuthResponseDto
{
    public int AdminId { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Token { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
}
