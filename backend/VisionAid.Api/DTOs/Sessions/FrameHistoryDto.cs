namespace VisionAid.Api.DTOs.Sessions;

public class FrameHistoryDto
{
    public int FrameId { get; set; }

    // Original frame
    public string OriginalImageName { get; set; } = null!;
    public string OriginalImagePath { get; set; } = null!;

    public DateTime CapturedAt { get; set; }

    // Analysis
    public int? AnalyzedFrameId { get; set; }
    public string? AnalyzedImagePath { get; set; }
    public DateTime? AnalyzedAt { get; set; }

    // Extracted from analysis.json
    public int? ImageWidth { get; set; }
    public int? ImageHeight { get; set; }
    public string? Question { get; set; }
    public string? Answer { get; set; }
}