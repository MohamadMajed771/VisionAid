namespace VisionAid.Api.Entities;

public class Answer
{
    public long Id { get; set; }

    public long QuestionId { get; set; }

    public required string AnswerText { get; set; }

    public required string Language { get; set; }

    public string ConfidenceStatus { get; set; } = "Uncertain";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Question Question { get; set; } = null!;

    public ICollection<AnswerFrame> AnswerFrames { get; set; }
        = new List<AnswerFrame>();
}