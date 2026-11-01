using Microsoft.AspNetCore.Http;
using VisionAid.Api.DTOs.Frames;

namespace VisionAid.Api.Services.Interfaces;

public interface IFrameService
{
    Task<UploadFrameResponseDto> UploadFrameAsync(
        int sessionId,
        IFormFile image);

    Task<string?> ProcessAnalysisResultAsync(
        int frameId,
        Stream zipStream);
}