
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
        // 1. Find or create the demo Admin.
        var admin = await context.Admins
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

            await context.SaveChangesAsync();
        }

        // 2. Find or create each demo User.
        foreach (var (name, email) in SeedUsers)
        {
            var user = await context.Users
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

                await context.SaveChangesAsync();
            }

            // 3. Find or create the Admin-User access request.
            var accessRequest = await context.AdminAccessRequests
                .FirstOrDefaultAsync(r =>
                    r.AdminId == admin.AdminId &&
                    r.UserId == user.UserId);

            if (accessRequest is null)
            {
                accessRequest = new AdminAccessRequest
                {
                    AdminId = admin.AdminId,
                    UserId = user.UserId,
                    Status = "Accepted",
                    RequestedAt = DateTime.UtcNow,
                    RespondedAt = DateTime.UtcNow
                };

                context.AdminAccessRequests.Add(accessRequest);
            }
            else if (accessRequest.Status != "Accepted")
            {
                // Ensure the demo Admin has access to demo Users.
                accessRequest.Status = "Accepted";
                accessRequest.RespondedAt = DateTime.UtcNow;
            }

            // 4. Create a demo session if the User has none.
            if (!user.StreamSessions.Any())
            {
                user.StreamSessions.Add(new StreamSession
                {
                    StartedAt = DateTime.UtcNow,
                    Status = "Active"
                });
            }
        }

        // 5. Save all remaining changes.
        await context.SaveChangesAsync();
    }
}
