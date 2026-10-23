namespace VisionAid.Api.Entities;

public class Question
{
    public long Id { get; set; }

    public long SessionId { get; set; }

    public required string QuestionText { get; set; }

    public DateTime AskedAt { get; set; } = DateTime.UtcNow;

    public StreamSession Session { get; set; } = null!;

    public Answer? Answer { get; set; }
}