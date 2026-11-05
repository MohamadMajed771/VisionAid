using Microsoft.AspNetCore.Http;

namespace VisionAid.Api.DTOs.Frames;

public class UploadFrameRequestDto
{
    public IFormFile Image { get; set; } = null!;
    public string? Question { get; set; }
}