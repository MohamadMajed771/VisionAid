using VisionAid.Api.DTOs.Admins;

namespace VisionAid.Api.Services.Interfaces;

public interface IAdminService
{
    Task<AdminAuthResponseDto> RegisterAsync(RegisterAdminRequestDto request);

    Task<AdminAuthResponseDto> LoginAsync(AdminLoginRequestDto request);

    Task<AdminResponseDto?> GetByIdAsync(int adminId);

    Task<IReadOnlyList<AdminUserResponseDto>> GetUsersAsync(int adminId);

    Task<AccessRequestResponseDto> CreateAccessRequestAsync(
        int adminId,
        CreateAccessRequestDto request);

    Task<IReadOnlyList<AccessRequestResponseDto>> GetAccessRequestsAsync(
        int adminId);

    Task<IReadOnlyList<AccessRequestResponseDto>> GetPendingRequestsAsync(
        int userId);

    Task<bool?> RespondToAccessRequestAsync(
        int requestId,
        int userId,
        bool accept);

    Task<bool> RevokeAdminAsync(int userId, int adminId);
}
