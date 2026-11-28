using Microsoft.AspNetCore.Http;
using VisionAid.Api.DTOs.Frames;
using VisionAid.Api.DTOs.Sessions;

namespace VisionAid.Api.Services.Interfaces;

public interface IFrameService
{
Task<int> ValidateFrameUploadAsync(
    int userId,
    IFormFile image);
   Task<string?> ProcessAnalysisResultAsync(
    int sessionId,
    IFormFile originalImage,
    DateTime capturedAt,
    Stream zipStream);

    Task<SessionHistoryPageDto?> GetHistoryForAdminAsync(
        int sessionId,
        int userId,
        int adminId,
        int page,
        int pageSize);
}