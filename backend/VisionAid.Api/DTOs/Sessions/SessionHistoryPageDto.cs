
namespace VisionAid.Api.DTOs.Sessions;

public class SessionHistoryPageDto
{
    public List<FrameHistoryDto> Frames { get; set; } = new();

    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalFrames { get; set; }
    public int TotalPages { get; set; }
}
