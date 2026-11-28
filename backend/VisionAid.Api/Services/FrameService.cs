
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

public async Task<int> ValidateFrameUploadAsync(
    int userId,
    IFormFile image)
{
    // 1. Find the user's active session.
    var session = await _context.StreamSessions
        .FirstOrDefaultAsync(s =>
            s.UserId == userId &&
            s.Status == "Active");

    if (session is null)
        throw new KeyNotFoundException(
            "No active session found.");

    // 2. Validate the image.
    if (image is null || image.Length == 0)
        throw new ArgumentException(
            "The uploaded image is empty.");

    // 3. Return the active session ID.
    // Nothing is saved to disk or database here.
    return session.SessionId;
}


public async Task<string?> ProcessAnalysisResultAsync(
    int sessionId,
    IFormFile originalImage,
    DateTime capturedAt,
    Stream zipStream)
{
    // 1. Verify the session exists.
    var session = await _context.StreamSessions
        .FirstOrDefaultAsync(s => s.SessionId == sessionId);

    if (session is null)
        throw new KeyNotFoundException("Session not found.");

    // 2. Resolve the backend folder.
    var backendFolder = Path.GetFullPath(
        Path.Combine(_environment.ContentRootPath, ".."));

    // 3. Create upload folders.
    var originalFolder = Path.Combine(
        backendFolder, "uploads", "frames");

    var analyzedImagesFolder = Path.Combine(
        backendFolder, "uploads", "analyzed-frames");

    var analysisFolder = Path.Combine(
        backendFolder, "uploads", "analysis");

    Directory.CreateDirectory(originalFolder);
    Directory.CreateDirectory(analyzedImagesFolder);
    Directory.CreateDirectory(analysisFolder);

    // 4. Generate a shared filename.
    var extension = Path.GetExtension(originalImage.FileName);

    if (string.IsNullOrWhiteSpace(extension))
        extension = ".jpg";

    var fileName =
        $"{capturedAt:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid():N}" +
        extension;

    var fileNameWithoutExtension =
        Path.GetFileNameWithoutExtension(fileName);

    var analyzedImageFileName =
        $"{fileNameWithoutExtension}.jpg";

    var jsonFileName =
        $"{fileNameWithoutExtension}.json";

    // 5. Build full paths.
    var originalImageFullPath = Path.Combine(
        originalFolder, fileName);

    var analyzedImageFullPath = Path.Combine(
        analyzedImagesFolder, analyzedImageFileName);

    var jsonFullPath = Path.Combine(
        analysisFolder, jsonFileName);

    // 6. Open ZIP returned by Python.
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

    // 7. Deserialize analysis JSON before saving anything.
    FrameAnalysisResultDto? analysisResult;

    await using (var jsonEntryStream = jsonEntry.Open())
    {
        analysisResult =
            await JsonSerializer.DeserializeAsync<FrameAnalysisResultDto>(
                jsonEntryStream,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
    }

    if (analysisResult is null)
        throw new InvalidDataException(
            "Could not deserialize the analysis JSON.");

    // 8. Save original image.
    await using (var originalStream = originalImage.OpenReadStream())
    await using (var fileStream = new FileStream(
        originalImageFullPath,
        FileMode.CreateNew,
        FileAccess.Write))
    {
        await originalStream.CopyToAsync(fileStream);
    }

    // 9. Save annotated image.
    await using (var imageEntryStream = imageEntry.Open())
    await using (var fileStream = new FileStream(
        analyzedImageFullPath,
        FileMode.Create,
        FileAccess.Write))
    {
        await imageEntryStream.CopyToAsync(fileStream);
    }

    // 10. Save analysis JSON.
    await using (var jsonEntryStream = jsonEntry.Open())
    await using (var fileStream = new FileStream(
        jsonFullPath,
        FileMode.Create,
        FileAccess.Write))
    {
        await jsonEntryStream.CopyToAsync(fileStream);
    }

    // 11. Build relative paths.
    var originalImageRelativePath = Path.Combine(
        "uploads", "frames", fileName);

    var analyzedImageRelativePath = Path.Combine(
        "uploads", "analyzed-frames", analyzedImageFileName);

    var jsonRelativePath = Path.Combine(
        "uploads", "analysis", jsonFileName);

    // 12. Create Frame only after successful analysis.
    var frame = new Frame
    {
        SessionId = sessionId,
        ImagePath = originalImageRelativePath,
        CapturedAt = capturedAt,
        CreatedAt = DateTime.UtcNow
    };

    _context.Frames.Add(frame);
    await _context.SaveChangesAsync();

    // 13. Save analysis and related DB data.
    await SaveAnalysisToDatabaseAsync(
        frame,
        analysisResult,
        analyzedImageRelativePath,
        jsonRelativePath);

    // 14. Return answer for Android TTS.
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
    
public async Task<SessionHistoryPageDto?> GetHistoryForAdminAsync(
    int sessionId,
    int userId,
    int adminId,
    int page,
    int pageSize)
{
    return await GetHistoryForAdminCoreAsync(
        sessionId,
        userId,
        page,
        pageSize,
        adminId);
}

private async Task<SessionHistoryPageDto?> GetHistoryForAdminCoreAsync(
    int sessionId,
    int userId,
    int page,
    int pageSize,
    int adminId)
{
    // 1. Validate pagination.
    if (page < 1)
        throw new ArgumentException("Page must be at least 1.");

var sessionExists = await _context.StreamSessions
    .AsNoTracking()
    .AnyAsync(s =>
        s.SessionId == sessionId &&
        s.UserId == userId &&
        _context.AdminAccessRequests.Any(r =>
            r.AdminId == adminId &&
            r.UserId == userId &&
            r.Status == "Accepted"));

if (!sessionExists)
    return null;
    
    // 3. Count the original frames.
    var frameQuery = _context.Frames
    .AsNoTracking()
    .Where(f =>
        f.SessionId == sessionId &&
        f.Session.UserId == userId &&
        _context.AdminAccessRequests.Any(r =>
            r.AdminId == adminId &&
            r.UserId == userId &&
            r.Status == "Accepted"));


    var totalFrames = await frameQuery.CountAsync();

    // 4. Retrieve the requested page and eager-load analyzed frames.
    var frames = await frameQuery
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

    FrameAnalysisResultDto? analysisResult = null;

    try
    {
        await using var jsonStream = new FileStream(
            jsonFullPath,
            FileMode.Open,
            FileAccess.Read);

        analysisResult =
            await JsonSerializer.DeserializeAsync<FrameAnalysisResultDto>(
                jsonStream,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
    }
    catch (FileNotFoundException)
    {
        Console.WriteLine(
            $"Analysis JSON missing for frame {frame.FrameId}.");
    }
    catch (DirectoryNotFoundException)
    {
        Console.WriteLine(
            $"Analysis directory missing for frame {frame.FrameId}.");
    }
    catch (JsonException)
    {
        Console.WriteLine(
            $"Invalid analysis JSON for frame {frame.FrameId}.");
    }

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
        ImageWidth = analysisResult?.ImageWidth,
        ImageHeight = analysisResult?.ImageHeight,
        Question = analysisResult?.Question,
        Answer = analysisResult?.Answer
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
