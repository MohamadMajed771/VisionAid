using Microsoft.AspNetCore.Http;
using VisionAid.Api.DTOs.Frames;
using VisionAid.Api.DTOs.Sessions;

namespace VisionAid.Api.Services.Interfaces;

public interface IFrameService
{
    Task<UploadFrameResponseDto> UploadFrameAsync(
        int sessionId,
        int userId,
        IFormFile image);

    Task<string?> ProcessAnalysisResultAsync(
        int frameId,
        Stream zipStream);

    Task<SessionHistoryPageDto?> GetHistoryAsync(
        int sessionId,
        int userId,
        int page,
        int pageSize);
}