
using VisionAid.Api.DTOs.Sessions;

namespace VisionAid.Api.Services.Interfaces;

public interface ISessionService
{
    Task<SessionResponseDto?> CreateAsync(int userId);

    Task<SessionResponseDto?> GetActiveAsync(int userId);

    Task<SessionResponseDto?> GetByIdAsync(
        int sessionId,
        int userId);

    Task<bool> EndAsync(int sessionId, int userId);

    
}
