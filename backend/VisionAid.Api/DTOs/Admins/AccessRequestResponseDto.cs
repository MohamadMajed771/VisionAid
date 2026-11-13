namespace VisionAid.Api.DTOs.Admins;

public class AccessRequestResponseDto
{
    public int RequestId { get; set; }
    public int AdminId { get; set; }
    public string AdminName { get; set; } = null!;
    public string AdminEmail { get; set; } = null!;
    public int UserId { get; set; }
    public string UserName { get; set; } = null!;
    public string UserEmail { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime RequestedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
}
