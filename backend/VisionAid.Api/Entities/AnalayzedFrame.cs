namespace VisionAid.Api.Models;

public class AnalyzedFrame
{
    public int AnalyzedFrameId { get; set; }
    public int FrameId { get; set; }

    public string AnalyzedImagePath { get; set; } =null!;
    public string JsonFilePath { get; set; } = null!;
   
    public DateTime AnalyzedAt { get; set; }

    public Frame Frame { get; set; } = null!;

    public ICollection<DetectedObject> DetectedObjects { get; set; }
        = new List<DetectedObject>();

    public ICollection<OcrResult> OcrResults { get; set; }
        = new List<OcrResult>();

    public ICollection<Answer> Answers { get; set; }
        = new List<Answer>();
}