
namespace VisionAid.Api.DTOs.Sessions;

public class SessionResponseDto
{
    public int SessionId { get; set; }
    public int UserId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}
