namespace VisionAid.Api.DTOs.Analysis;

public class FrameAnalysisResultDto
{
    public string Image { get; set; } = null!;
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }

    public string DepthModel { get; set; } = null!;
    public string DepthPrecision { get; set; } = null!;

    public List<DetectedObjectDto> DetectedObjects { get; set; } = new();
    public List<OcrResultDto> OcrResults { get; set; } = new();

    public string? Question { get; set; }
    public string? Answer { get; set; }
}