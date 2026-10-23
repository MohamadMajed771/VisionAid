namespace VisionAid.Api.Entities;

public class Frame
{
    public long Id { get; set; }

    public long SessionId { get; set; }

    public required string ImagePath { get; set; }

    public DateTime CapturedAt { get; set; }

    public double? SharpnessScore { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public StreamSession Session { get; set; } = null!;

    public ICollection<OcrResult> OcrResults { get; set; }
        = new List<OcrResult>();

    public ICollection<DetectedObject> DetectedObjects { get; set; }
        = new List<DetectedObject>();

    public ICollection<AnswerFrame> AnswerFrames { get; set; }
        = new List<AnswerFrame>();
}