namespace VisionAid.Api.Models;

public class Question
{
    public int QuestionId { get; set; }
    public int SessionId { get; set; }

    public DateTime AskedAt { get; set; }
    public string QuestionText { get; set; } = null!;
    public StreamSession Session { get; set; } = null!;

    public Answer? Answer { get; set; }
}