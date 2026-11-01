
using VisionAid.Api.DTOs.Users;

namespace VisionAid.Api.Services.Interfaces;

public interface IUserService
{
    Task<UserResponseDto?> CreateAsync(CreateUserRequestDto request);

    Task<UserResponseDto?> GetByIdAsync(int userId);
}
