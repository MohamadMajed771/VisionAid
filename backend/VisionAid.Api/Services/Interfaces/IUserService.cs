
using VisionAid.Api.DTOs.Users;

namespace VisionAid.Api.Services.Interfaces;

public interface IUserService
{
    Task<UserResponseDto?> CreateAsync(CreateUserRequestDto request);

   Task<string> UpdatePreferredLanguageAsync(int userId, string language);
    Task<UserResponseDto?> GetByIdAsync(int userId);
}
