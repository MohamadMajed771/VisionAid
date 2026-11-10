using VisionAid.Api.DTOs.Sessions;

namespace VisionAid.Api.DTOs.Admins;

/// <summary>A user associated with an admin.</summary>
public class AdminUserResponseDto
{
    public int UserId { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public List<SessionResponseDto> Sessions { get; set; } = new();
}