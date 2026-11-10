using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VisionAid.Api.Models;

namespace VisionAid.Api.Data;

public static class DevelopmentDataSeeder
{
    // These fixture credentials are seeded only in the Development environment.
    private const string AdminEmail = "admin@visionaid.local";
    private const string AdminPassword = "Admin123!";

    private static readonly (string Name, string Email)[] SeedUsers =
    [
        ("Demo User One", "user1@visionaid.local"),
        ("Demo User Two", "user2@visionaid.local"),
        ("Demo User Three", "user3@visionaid.local")
    ];

    public static async Task SeedAsync(
        AppDbContext context,
        IPasswordHasher<User> userPasswordHasher,
        IPasswordHasher<Admin> adminPasswordHasher)
    {
        var admin = await context.Admins
            .Include(a => a.Users)
            .FirstOrDefaultAsync(a => a.Email == AdminEmail);

        if (admin is null)
        {
            admin = new Admin
            {
                Name = "VisionAid Demo Admin",
                Email = AdminEmail,
                CreatedAt = DateTime.UtcNow
            };
            admin.PasswordHash = adminPasswordHasher.HashPassword(
                admin,
                AdminPassword);
            context.Admins.Add(admin);
        }

        foreach (var (name, email) in SeedUsers)
        {
            var user = await context.Users
                .Include(u => u.Admins)
                .Include(u => u.StreamSessions)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user is null)
            {
                user = new User
                {
                    Name = name,
                    Email = email,
                    PreferredLanguage = "en",
                    CreatedAt = DateTime.UtcNow
                };
                user.PasswordHash = userPasswordHasher.HashPassword(
                    user,
                    "User123!");
                context.Users.Add(user);
            }

            var accessRequest = await context.AdminAccessRequests
                .FirstOrDefaultAsync(r =>
                    r.Admin.Email == AdminEmail &&
                    r.User.Email == email);

            if (accessRequest is null)
            {
                accessRequest = new AdminAccessRequest
                {
                    Admin = admin,
                    User = user,
                    Status = "Accepted",
                    RequestedAt = DateTime.UtcNow,
                    RespondedAt = DateTime.UtcNow
                };
                context.AdminAccessRequests.Add(accessRequest);
            }

            if (accessRequest.Status == "Accepted" &&
                !admin.Users.Any(u => u.Email == email))
            {
                admin.Users.Add(user);
            }

            if (!user.StreamSessions.Any())
            {
                user.StreamSessions.Add(new StreamSession
                {
                    StartedAt = DateTime.UtcNow,
                    Status = "Active"
                });
            }
        }

        await context.SaveChangesAsync();
    }
}
