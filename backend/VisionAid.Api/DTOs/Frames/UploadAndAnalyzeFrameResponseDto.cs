namespace Visionaid.Api.DTOs.Frames;

public class UploadAndAnalyzeFrameResponseDto
{
    public int FrameId { get; set; }
    public int SessionId { get; set; }
    public string? Answer { get; set; }
    public string Language { get; set; } = "en";
    public DateTime CapturedAt { get; set; }
}