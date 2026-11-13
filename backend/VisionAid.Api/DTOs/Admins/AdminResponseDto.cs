namespace VisionAid.Api.DTOs.Admins;

/// <summary>An admin and the users associated with that admin.</summary>
public class AdminResponseDto
{
    public int AdminId { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public IReadOnlyList<AdminUserResponseDto> Users { get; set; }
        = new List<AdminUserResponseDto>();
}