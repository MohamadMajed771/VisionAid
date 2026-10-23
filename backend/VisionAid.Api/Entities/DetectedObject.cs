
namespace VisionAid.Api.Entities;

public class DetectedObject
{
    public long Id { get; set; }

    public long FrameId { get; set; }

    public required string Label { get; set; }

    public int? ClassId { get; set; }

    public double Confidence { get; set; }

    public required string BoundingBox { get; set; }

    public double? DepthEstimate { get; set; }

    public string? DepthUnit { get; set; }

    public Frame Frame { get; set; } = null!;
}
