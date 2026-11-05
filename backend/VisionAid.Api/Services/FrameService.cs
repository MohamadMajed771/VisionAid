
using System.IO.Compression;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VisionAid.Api.Data;
using VisionAid.Api.DTOs.Analysis;
using VisionAid.Api.DTOs.Frames;
using VisionAid.Api.DTOs.Sessions;
using VisionAid.Api.Models;
using VisionAid.Api.Services.Interfaces;


namespace VisionAid.Api.Services;

public class FrameService : IFrameService
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public FrameService(
        AppDbContext context,
        IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    public async Task<UploadFrameResponseDto> UploadFrameAsync(
        int sessionId,
        int userId,
        IFormFile image)
    {
        // 1. Find the session and verify ownership.
        var session = await _context.StreamSessions
            .FirstOrDefaultAsync(s =>
                s.SessionId == sessionId &&
                s.UserId == userId);

        if (session is null)
            throw new KeyNotFoundException(
                "Session not found.");

        // 2. Make sure the session is active.
        if (session.Status != "Active")
            throw new InvalidOperationException(
                "Cannot upload frames to an ended session.");

        // 3. Validate the uploaded image.
        if (image is null || image.Length == 0)
            throw new ArgumentException(
                "The uploaded image is empty.");

        // 4. Get Backend folder (parent of VisionAid.Api).
        var backendFolder = Path.GetFullPath(
            Path.Combine(_environment.ContentRootPath, ".."));

        // 5. Create original images folder.
        var uploadFolder = Path.Combine(
            backendFolder,
            "uploads",
            "frames");

        Directory.CreateDirectory(uploadFolder);

        // 6. Generate a unique filename.
        var capturedAt = DateTime.UtcNow;

        var fileName =
            $"{capturedAt:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid():N}" +
            Path.GetExtension(image.FileName);

        // 7. Save original image.
        var fullPath = Path.Combine(uploadFolder, fileName);

        await using (var stream = new FileStream(
            fullPath,
            FileMode.CreateNew,
            FileAccess.Write))
        {
            await image.CopyToAsync(stream);
        }

        // 8. Store relative path.
        var relativePath = Path.Combine(
            "uploads",
            "frames",
            fileName);

        var frame = new Frame
        {
            SessionId = sessionId,
            ImagePath = relativePath,
            CapturedAt = capturedAt,
            CreatedAt = DateTime.UtcNow
        };

        _context.Frames.Add(frame);
        await _context.SaveChangesAsync();

        return new UploadFrameResponseDto
        {
            FrameId = frame.FrameId,
            SessionId = frame.SessionId,
            ImagePath = frame.ImagePath,
            CapturedAt = frame.CapturedAt
        };
    }

    public async Task<string?> ProcessAnalysisResultAsync(
        int frameId,
        Stream zipStream)
    {
        // 1. Find the original frame.
        var frame = await _context.Frames
            .FirstOrDefaultAsync(f => f.FrameId == frameId);

        if (frame is null)
            throw new KeyNotFoundException("Frame not found.");

        // 2. Get Backend folder.
        var backendFolder = Path.GetFullPath(
            Path.Combine(_environment.ContentRootPath, ".."));

        // 3. Create folders for analysis files.
        var analyzedImagesFolder = Path.Combine(
            backendFolder,
            "uploads",
            "analyzed-frames");

        var analysisFolder = Path.Combine(
            backendFolder,
            "uploads",
            "analysis");

        Directory.CreateDirectory(analyzedImagesFolder);
        Directory.CreateDirectory(analysisFolder);

        // 4. Use original image filename as common filename.
        var originalFileName = Path.GetFileName(frame.ImagePath);

        var fileNameWithoutExtension =
            Path.GetFileNameWithoutExtension(originalFileName);

        var analyzedImageFileName =
            $"{fileNameWithoutExtension}.jpg";

        var jsonFileName =
            $"{fileNameWithoutExtension}.json";

        var analyzedImageFullPath = Path.Combine(
            analyzedImagesFolder,
            analyzedImageFileName);

        var jsonFullPath = Path.Combine(
            analysisFolder,
            jsonFileName);

        // 5. Open ZIP returned by FastAPI.
        using var archive = new ZipArchive(
            zipStream,
            ZipArchiveMode.Read,
            leaveOpen: true);

        var imageEntry = archive.GetEntry("annotated.jpg");
        var jsonEntry = archive.GetEntry("analysis.json");

        if (imageEntry is null || jsonEntry is null)
        {
            throw new InvalidDataException(
                "The ZIP must contain annotated.jpg and analysis.json.");
        }

        // 6. Save annotated image.
        await using (var imageStream = imageEntry.Open())
        await using (var fileStream = new FileStream(
            analyzedImageFullPath,
            FileMode.Create,
            FileAccess.Write))
        {
            await imageStream.CopyToAsync(fileStream);
        }

        // 7. Save original JSON file.
        await using (var jsonStream = jsonEntry.Open())
        await using (var fileStream = new FileStream(
            jsonFullPath,
            FileMode.Create,
            FileAccess.Write))
        {
            await jsonStream.CopyToAsync(fileStream);
        }

        // 8. Deserialize JSON into DTO.
        FrameAnalysisResultDto? analysisResult;

        await using (var jsonStream = new FileStream(
            jsonFullPath,
            FileMode.Open,
            FileAccess.Read))
        {
            analysisResult =
                await JsonSerializer.DeserializeAsync<FrameAnalysisResultDto>(
                    jsonStream,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
        }

        if (analysisResult is null)
            throw new InvalidDataException(
                "Could not deserialize the analysis JSON.");

        // 9. Convert file paths to relative paths.
        var analyzedImageRelativePath = Path.Combine(
            "uploads",
            "analyzed-frames",
            analyzedImageFileName);

        var jsonRelativePath = Path.Combine(
            "uploads",
            "analysis",
            jsonFileName);

        // 10. Save analysis results in database.
        await SaveAnalysisToDatabaseAsync(
            frame,
            analysisResult,
            analyzedImageRelativePath,
            jsonRelativePath);

        // 11. Return generated answer for Android TTS.
        return analysisResult.Answer;
    }

    private async Task SaveAnalysisToDatabaseAsync(
        Frame frame,
        FrameAnalysisResultDto analysisResult,
        string analyzedImagePath,
        string jsonFilePath)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            // Create AnalyzedFrame.
            var analyzedFrame = new AnalyzedFrame
            {
                FrameId = frame.FrameId,
                AnalyzedImagePath = analyzedImagePath,
                JsonFilePath = jsonFilePath,
                AnalyzedAt = DateTime.UtcNow
            };

            _context.AnalyzedFrames.Add(analyzedFrame);
            await _context.SaveChangesAsync();

            // Save detected objects.
            foreach (var detectedObject in analysisResult.DetectedObjects)
            {
                _context.DetectedObjects.Add(new DetectedObject
                {
                    AnalyzedFrameId = analyzedFrame.AnalyzedFrameId,
                    Label = detectedObject.Label,
                    Confidence = detectedObject.Confidence,
                    Depth = detectedObject.Depth,
                    X1 = detectedObject.X1,
                    Y1 = detectedObject.Y1,
                    X2 = detectedObject.X2,
                    Y2 = detectedObject.Y2
                });
            }

            // Save OCR results.
            foreach (var ocrResult in analysisResult.OcrResults)
            {
                _context.OCRResults.Add(new OcrResult
                {
                    AnalyzedFrameId = analyzedFrame.AnalyzedFrameId,
                    ExtractedText = ocrResult.ExtractedText,
                    Confidence = ocrResult.Confidence
                });
            }

            // Create Question only when one exists.
            if (!string.IsNullOrWhiteSpace(analysisResult.Question))
            {
                var question = new Question
                {
                    SessionId = frame.SessionId,
                    QuestionText = analysisResult.Question,
                    AskedAt = DateTime.UtcNow
                };

                _context.Questions.Add(question);
                await _context.SaveChangesAsync();

                // Create Answer only when one exists.
                if (!string.IsNullOrWhiteSpace(analysisResult.Answer))
                {
                    _context.Answers.Add(new Answer
                    {
                        QuestionId = question.QuestionId,
                        AnalyzedFrameId = analyzedFrame.AnalyzedFrameId,
                        AnswerText = analysisResult.Answer,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    
public async Task<SessionHistoryPageDto?> GetHistoryAsync(
    int sessionId,
    int userId,
    int page,
    int pageSize)
{
    // 1. Validate pagination.
    if (page < 1)
        throw new ArgumentException("Page must be at least 1.");


    // 2. Verify that the session belongs to the user.
    var sessionExists = await _context.StreamSessions
        .AnyAsync(s =>
            s.SessionId == sessionId &&
            s.UserId == userId);

    if (!sessionExists)
        return null;

    // 3. Count the original frames.
    var totalFrames = await _context.Frames
        .CountAsync(f => f.SessionId == sessionId);

    // 4. Retrieve the requested page and eager-load analyzed frames.
    var frames = await _context.Frames
        .AsNoTracking()
        .Where(f =>
            f.SessionId == sessionId &&
            f.Session.UserId == userId)
        .OrderBy(f => f.CapturedAt)
        .ThenBy(f => f.FrameId)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Include(f => f.AnalyzedFrames)
        .ToListAsync();

    // 5. Resolve the backend directory.
    var backendFolder = Path.GetFullPath(
        Path.Combine(_environment.ContentRootPath, ".."));

    var history = new List<FrameHistoryDto>();

    foreach (var frame in frames)
{
    if (frame.AnalyzedFrames.Count == 0)
    {
        history.Add(new FrameHistoryDto
        {
            FrameId = frame.FrameId,
            OriginalImageName = Path.GetFileName(frame.ImagePath),
            OriginalImagePath = frame.ImagePath,
            CapturedAt = frame.CapturedAt
        });

        continue;
    }

    foreach (var analyzedFrame in frame.AnalyzedFrames)
    {
        // Read JSON for Question, Answer and dimensions.
        var jsonFullPath = Path.GetFullPath(
            Path.Combine(
                backendFolder,
                analyzedFrame.JsonFilePath));

        FrameAnalysisResultDto? analysisResult;

        await using (var jsonStream = new FileStream(
            jsonFullPath,
            FileMode.Open,
            FileAccess.Read))
        {
            analysisResult =
                await JsonSerializer.DeserializeAsync<FrameAnalysisResultDto>(
                    jsonStream,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
        }

        if (analysisResult is null)
            throw new InvalidDataException(
                $"Could not deserialize analysis for frame {frame.FrameId}.");

        history.Add(new FrameHistoryDto
        {
            FrameId = frame.FrameId,

            // Database
            OriginalImageName = Path.GetFileName(frame.ImagePath),
            OriginalImagePath = frame.ImagePath,
            CapturedAt = frame.CapturedAt,

            // Database
            AnalyzedFrameId = analyzedFrame.AnalyzedFrameId,
            AnalyzedImagePath = analyzedFrame.AnalyzedImagePath,
            AnalyzedAt = analyzedFrame.AnalyzedAt,

            // JSON
            ImageWidth = analysisResult.ImageWidth,
            ImageHeight = analysisResult.ImageHeight,
            Question = analysisResult.Question,
            Answer = analysisResult.Answer
        });
    }
}
    // 9. Return paginated history.
    return new SessionHistoryPageDto
    {
        Frames = history,
        Page = page,
        PageSize = pageSize,
        TotalFrames = totalFrames,
        TotalPages = (int)Math.Ceiling(
            (double)totalFrames / pageSize)
    };
}

    
}
