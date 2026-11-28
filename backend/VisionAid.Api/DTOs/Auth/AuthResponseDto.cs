namespace VisionAid.Api.DTOs;

public class AuthResponse
{
    public int UserId { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Token { get; set; } = null!;
    public string PreferredLanguage { get; set; } = "en";
    public DateTime ExpiresAt { get; set; }
}