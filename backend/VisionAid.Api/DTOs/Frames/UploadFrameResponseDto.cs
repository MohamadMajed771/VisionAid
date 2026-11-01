
namespace VisionAid.Api.DTOs.Frames;

public class UploadFrameResponseDto
{
    public int FrameId { get; set; }
    public int SessionId { get; set; }
    public string ImagePath { get; set; } = null!;
    public double MinDepth { get; set; }
    public DateTime CapturedAt { get; set; }
}
