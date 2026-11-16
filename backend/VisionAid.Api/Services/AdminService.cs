using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using VisionAid.Api.Data;
using VisionAid.Api.DTOs.Admins;
using VisionAid.Api.Models;
using VisionAid.Api.Services.Interfaces;

namespace VisionAid.Api.Services;

public class AdminService : IAdminService
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher<Admin> _passwordHasher;
    private readonly IConfiguration _configuration;

    public AdminService(
        AppDbContext context,
        IPasswordHasher<Admin> passwordHasher,
        IConfiguration configuration)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
    }

    public async Task<AdminAuthResponseDto> RegisterAsync(
        RegisterAdminRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentException(
                "Name, email, and password are required.");
        }

        if (request.Password.Length < 8)
            throw new ArgumentException("Password must contain at least 8 characters.");

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _context.Admins.AnyAsync(a => a.Email == email))
            throw new InvalidOperationException(
                "An admin account with this email already exists.");

        var admin = new Admin
        {
            Name = request.Name.Trim(),
            Email = email,
            CreatedAt = DateTime.UtcNow
        };
        admin.PasswordHash = _passwordHasher.HashPassword(admin, request.Password);

        _context.Admins.Add(admin);
        await _context.SaveChangesAsync();

        return CreateAuthResponse(admin);
    }

    public async Task<AdminAuthResponseDto> LoginAsync(
        AdminLoginRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentException("Email and password are required.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var admin = await _context.Admins
            .FirstOrDefaultAsync(a => a.Email == email);

        if (admin is null ||
            _passwordHasher.VerifyHashedPassword(
                admin,
                admin.PasswordHash,
                request.Password) == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        return CreateAuthResponse(admin);
    }

   public async Task<AdminResponseDto?> GetByIdAsync(int adminId)
{
    var admin = await _context.Admins
        .AsNoTracking()
        .FirstOrDefaultAsync(a => a.AdminId == adminId);

    if (admin is null)
        return null;

    var users = await GetUsersAsync(adminId);

    return MapToResponse(admin, users);
}
public async Task<List<AdminUserResponseDto>> GetUsersAsync(
    int adminId)
{
    var users = await _context.Users
        .AsNoTracking()
        .Where(u =>
            _context.AdminAccessRequests.Any(r =>
                r.AdminId == adminId &&
                r.UserId == u.UserId &&
                r.Status == "Accepted"))
        .ToListAsync();

    return users
        .OrderBy(u => u.UserId)
        .Select(u => new AdminUserResponseDto
        {
            UserId = u.UserId,
            Name = u.Name,
            Email = u.Email
        })
        .ToList();
} 
    public async Task<AccessRequestResponseDto> CreateAccessRequestAsync(
        int adminId,
        CreateAccessRequestDto request)
    {
        var email = request.UserEmail.Trim().ToLowerInvariant();
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user is null)
            throw new KeyNotFoundException("No user was found with this email.");

var alreadyLinked = await _context.AdminAccessRequests
    .AnyAsync(r =>
        r.AdminId == adminId &&
        r.UserId == user.UserId &&
        r.Status == "Accepted");
        if (alreadyLinked)
            throw new InvalidOperationException(
                "This user has already granted access to this admin.");

        var accessRequest = await _context.AdminAccessRequests
            .Include(r => r.Admin)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r =>
                r.AdminId == adminId && r.UserId == user.UserId);

        if (accessRequest?.Status == "Pending")
            throw new InvalidOperationException(
                "An access request is already pending for this user.");

        if (accessRequest is null)
        {
            accessRequest = new AdminAccessRequest
            {
                AdminId = adminId,
                UserId = user.UserId,
                Admin = await _context.Admins
                    .FirstAsync(a => a.AdminId == adminId),
                User = user,
                RequestedAt = DateTime.UtcNow
            };
            _context.AdminAccessRequests.Add(accessRequest);
        }
        else
        {
            accessRequest.Status = "Pending";
            accessRequest.RequestedAt = DateTime.UtcNow;
            accessRequest.RespondedAt = null;
        }

        await _context.SaveChangesAsync();
        return MapToResponse(accessRequest);
    }

    public async Task<List<AccessRequestResponseDto>> GetAccessRequestsAsync(
        int adminId)
    {
        var requests = await _context.AdminAccessRequests
            .AsNoTracking()
            .Include(r => r.Admin)
            .Include(r => r.User)
            .Where(r => r.AdminId == adminId)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync();

        return requests.Select(MapToResponse).ToList();
    }

    public async Task<List<AccessRequestResponseDto>> GetPendingRequestsAsync(
        int userId)
    {
        var requests = await _context.AdminAccessRequests
            .AsNoTracking()
            .Include(r => r.Admin)
            .Include(r => r.User)
            .Where(r => r.UserId == userId && r.Status == "Pending")
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync();

        return requests.Select(MapToResponse).ToList();
    }
public async Task<bool?> RespondToAccessRequestAsync(
    int requestId,
    int userId,
    bool accept)
{
    var accessRequest = await _context.AdminAccessRequests
        .FirstOrDefaultAsync(r =>
            r.AdminAccessRequestId == requestId &&
            r.UserId == userId);

    if (accessRequest is null)
        return null;

    if (accessRequest.Status != "Pending")
        return false;

    accessRequest.Status = accept ? "Accepted" : "Declined";
    accessRequest.RespondedAt = DateTime.UtcNow;

    await _context.SaveChangesAsync();

    return true;
}
   public async Task<bool> RevokeAdminAsync(int userId, int adminId)
{
    var acceptedRequest = await _context.AdminAccessRequests
        .FirstOrDefaultAsync(r =>
            r.UserId == userId &&
            r.AdminId == adminId &&
            r.Status == "Accepted");

    if (acceptedRequest is null)
        return false;

    acceptedRequest.Status = "Revoked";
    acceptedRequest.RespondedAt = DateTime.UtcNow;

    await _context.SaveChangesAsync();

    return true;
}
    private AdminAuthResponseDto CreateAuthResponse(Admin admin)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(
            int.Parse(_configuration["Jwt:ExpiryMinutes"]!));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, admin.AdminId.ToString()),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Email, admin.Email),
            new Claim(ClaimTypes.Name, admin.Name)
        };
        var key = new SymmetricSecurityKey(
            Convert.FromBase64String(_configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new AdminAuthResponseDto
        {
            AdminId = admin.AdminId,
            Name = admin.Name,
            Email = admin.Email,
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = expiresAt
        };
    }
private static AdminResponseDto MapToResponse(
    Admin admin,
    List<AdminUserResponseDto> users)
{
    return new AdminResponseDto
    {
        AdminId = admin.AdminId,
        Name = admin.Name,
        Email = admin.Email,
        CreatedAt = new DateTimeOffset(
            DateTime.SpecifyKind(admin.CreatedAt, DateTimeKind.Utc)),
        Users = users
    };
}
    private static AccessRequestResponseDto MapToResponse(
        AdminAccessRequest request)
    {
        return new AccessRequestResponseDto
        {
            RequestId = request.AdminAccessRequestId,
            AdminId = request.AdminId,
            AdminName = request.Admin.Name,
            AdminEmail = request.Admin.Email,
            UserId = request.UserId,
            UserName = request.User.Name,
            UserEmail = request.User.Email,
            Status = request.Status,
            RequestedAt = request.RequestedAt,
            RespondedAt = request.RespondedAt
        };
    }

    private static AdminUserResponseDto MapToUserResponse(User user)
    {
        return new AdminUserResponseDto
        {
            UserId = user.UserId,
            Name = user.Name,
            Email = user.Email,
            Sessions = user.StreamSessions
                .OrderByDescending(session => session.StartedAt)
                .Select(session => new VisionAid.Api.DTOs.Sessions.SessionResponseDto
                {
                    SessionId = session.SessionId,
                    UserId = session.UserId,
                    StartedAt = session.StartedAt,
                    EndedAt = session.EndedAt,
                    Status = session.Status
                })
                .ToList()
        };
    }
}
