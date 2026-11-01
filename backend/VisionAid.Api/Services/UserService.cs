
using Microsoft.EntityFrameworkCore;
using VisionAid.Api.Data;
using VisionAid.Api.DTOs.Users;
using VisionAid.Api.Models;
using VisionAid.Api.Services.Interfaces;

namespace VisionAid.Api.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context)
    {
        _context = context;
    }

        public async Task<UserResponseDto   ?> CreateAsync(
        CreateUserRequestDto request)
    {
        var email = request.Email.Trim();

        var emailExists = await _context.Users
            .AnyAsync(u => u.Email == email);

        if (emailExists)
            return null;

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = email,
            PreferredLanguage = request.PreferredLanguage.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return MapToResponse(user);
    }

    public async Task<UserResponseDto   ?> GetByIdAsync(int userId)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId);

        return user is null ? null : MapToResponse(user);
    }

    private static UserResponseDto    MapToResponse(User user)
    {
        return new UserResponseDto
        {
            UserId = user.UserId,
            Name = user.Name,
            Email = user.Email,
            PreferredLanguage = user.PreferredLanguage,
            CreatedAt = user.CreatedAt
        };
    }
}
