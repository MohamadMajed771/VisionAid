namespace VisionAid.Api.Models;

public class OcrResult
{    public int OCRResultId { get; set; }
    public int AnalyzedFrameId { get; set; }

    public string ExtractedText { get; set; } = null!; //null on compile but not in db
    public float Confidence { get; set; }
    public AnalyzedFrame AnalyzedFrame { get; set; } = null!;
}