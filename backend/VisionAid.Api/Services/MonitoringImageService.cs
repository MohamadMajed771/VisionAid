using Microsoft.EntityFrameworkCore;
using VisionAid.Api.Data;

namespace VisionAid.Api.Services;

public record MonitoringImage(
    string FullPath,
    string ContentType);

public class MonitoringImageService
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public MonitoringImageService(
        AppDbContext context,
        IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    public async Task<MonitoringImage?> GetAsync(
        int adminId,
        int userId,
        int sessionId,
        int frameId,
        bool analyzed = false)
    {
        var frames = _context.Frames
            .AsNoTracking()
            .Where(f =>
                f.FrameId == frameId &&
                f.SessionId == sessionId &&
                f.Session.UserId == userId &&
                _context.AdminAccessRequests.Any(r =>
                    r.AdminId == adminId &&
                    r.UserId == userId &&
                    r.Status == "Accepted"));

        string? relativePath;

        if (analyzed)
        {
            relativePath = await frames
                .SelectMany(f => f.AnalyzedFrames)
                .Select(a => a.AnalyzedImagePath)
                .FirstOrDefaultAsync();
        }
        else
        {
            relativePath = await frames
                .Select(f => f.ImagePath)
                .FirstOrDefaultAsync();
        }

        if (string.IsNullOrWhiteSpace(relativePath))
            return null;

        var folder = analyzed
            ? "analyzed-frames"
            : "frames";

        var normalized = relativePath.Replace('\\', '/');

        var filename = Path.GetFileName(normalized);

        if (string.IsNullOrEmpty(filename) ||
            normalized != $"uploads/{folder}/{filename}")
        {
            return null;
        }

        var contentType =
            Path.GetExtension(filename)
                .ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => null
            };

        if (contentType is null)
            return null;

        var fullPath = Path.GetFullPath(
            Path.Combine(
                _environment.ContentRootPath,
                "..",
                "uploads",
                folder,
                filename));

        if (!File.Exists(fullPath))
            return null;

        return new MonitoringImage(
            fullPath,
            contentType);
    }
}