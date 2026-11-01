namespace VisionAid.Api.Models;

public class Frame
{
    public int FrameId { get; set; }
    public int SessionId { get; set; }

    public string ImagePath { get; set; } = null!;
    public DateTime CapturedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public StreamSession Session { get; set; } = null!;

    public ICollection<AnalyzedFrame> AnalyzedFrames { get; set; }
        = new List<AnalyzedFrame>();
}