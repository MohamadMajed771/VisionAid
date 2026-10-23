namespace VisionAid.Api.Entities;

public class StreamSession
{
    public long Id { get; set; }

    public long? UserId { get; set; }

    public required string ClientType { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? EndedAt { get; set; }

    public string Status { get; set; } = "Active";

    public User? User { get; set; }

    public ICollection<Frame> Frames { get; set; }
        = new List<Frame>();

    public ICollection<Question> Questions { get; set; }
        = new List<Question>();
}