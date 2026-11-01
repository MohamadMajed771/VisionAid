namespace VisionAid.Api.Models;

public class DetectedObject
{
    public int DetectedObjectId { get; set; }
    public int AnalyzedFrameId { get; set; }

    public string Label { get; set; } = null!;
    public float Confidence { get; set; }
    public float Depth { get; set; }

public float X1 { get; set; }
public float Y1 { get; set; }
public float X2 { get; set; }
public float Y2 { get; set; }

    public AnalyzedFrame AnalyzedFrame { get; set; } = null!;
}