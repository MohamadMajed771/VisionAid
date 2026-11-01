namespace VisionAid.Api.Models;

public class StreamSession
{
    public int SessionId { get; set; }
    public int UserId { get; set; }

    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string Status { get; set; } = "Active";

    public User User { get; set; } = null!;

    public ICollection<Frame> Frames { get; set; }
        = new List<Frame>();

    public ICollection<Question> Questions { get; set; }
        = new List<Question>();
}