namespace VisionAid.Api.DTOs.Admins;

/// <summary>An admin associated with a user.</summary>
public class AdminSummaryResponseDto
{
    public int AdminId { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
}