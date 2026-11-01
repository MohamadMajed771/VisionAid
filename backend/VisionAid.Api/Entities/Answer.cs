namespace VisionAid.Api.Models;

public class Answer
{
    public int AnswerId { get; set; }
    public int QuestionId { get; set; }
    public int AnalyzedFrameId { get; set; }

    public string AnswerText { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public Question Question { get; set; } = null!;
    public AnalyzedFrame AnalyzedFrame { get; set; } = null!;
}