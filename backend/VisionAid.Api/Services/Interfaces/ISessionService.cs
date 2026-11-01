
using VisionAid.Api.DTOs.Sessions;

namespace VisionAid.Api.Services.Interfaces;

public interface ISessionService
{
    Task<SessionResponseDto?> CreateAsync(
        CreateSessionRequestDto request);

    Task<SessionResponseDto ?> GetByIdAsync(int sessionId);

    Task<bool> EndAsync(int sessionId);
}
