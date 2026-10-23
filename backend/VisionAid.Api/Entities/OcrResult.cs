namespace VisionAid.Api.Entities;

public class OcrResult
{
    public long Id { get; set; }

    public long FrameId { get; set; }

    public required string ExtractedText { get; set; }

    public double? Confidence { get; set; }

    public string? BoundingBox { get; set; }

    public Frame Frame { get; set; } = null!;
}