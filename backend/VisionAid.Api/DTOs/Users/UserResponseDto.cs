
namespace VisionAid.Api.DTOs.Users;

public class UserResponseDto
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PreferredLanguage { get; set; } = "en";
    public DateTime CreatedAt { get; set; }
}
