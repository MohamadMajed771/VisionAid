namespace VisionAid.Api.Models;

public class AdminAccessRequest
{
    public int AdminAccessRequestId { get; set; }
    public int AdminId { get; set; }
    public int UserId { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime RequestedAt { get; set; }
    public DateTime? RespondedAt { get; set; }

    public Admin Admin { get; set; } = null!;
    public User User { get; set; } = null!;
}
