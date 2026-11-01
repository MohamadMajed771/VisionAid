using System.IO.Compression;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VisionAid.Api.Data;
using VisionAid.Api.DTOs.Analysis;
using VisionAid.Api.DTOs.Frames;
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
        IFormFile image)
    {
        var session = await _context.StreamSessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        if (session is null)
            throw new KeyNotFoundException("Session not found.");

        if (session.Status != "Active")
            throw new InvalidOperationException(
                "Cannot upload frames to an ended session.");

        if (image.Length == 0)
            throw new ArgumentException("The uploaded image is empty.");

        // Get Backend folder (parent of VisionAid.Api).
        var backendFolder = Path.GetFullPath(
            Path.Combine(_environment.ContentRootPath, ".."));

        // Create original images folder.
        var uploadFolder = Path.Combine(
            backendFolder,
            "uploads",
            "frames");

        Directory.CreateDirectory(uploadFolder);

        // Generate timestamp-based filename.
        var capturedAt = DateTime.UtcNow;

        var fileName =
            $"{capturedAt:yyyyMMdd_HHmmss_fff}{Path.GetExtension(image.FileName)}";

        // Save original image.
        var fullPath = Path.Combine(uploadFolder, fileName);

        await using (var stream = new FileStream(
            fullPath,
            FileMode.CreateNew,
            FileAccess.Write))
        {
            await image.CopyToAsync(stream);
        }

        // Store relative path.
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

        // 3. Create folders for the analysis files.
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

        // 4. Use the original image filename as the common filename.
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

        // 5. Open the ZIP returned by FastAPI.
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

        // 6. Save the annotated image.
        await using (var imageStream = imageEntry.Open())
        await using (var fileStream = new FileStream(
            analyzedImageFullPath,
            FileMode.Create,
            FileAccess.Write))
        {
            await imageStream.CopyToAsync(fileStream);
        }

        // 7. Save the original JSON file.
        await using (var jsonStream = jsonEntry.Open())
        await using (var fileStream = new FileStream(
            jsonFullPath,
            FileMode.Create,
            FileAccess.Write))
        {
            await jsonStream.CopyToAsync(fileStream);
        }

        // 8. Deserialize the JSON file into our DTO.
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

        // 9. Convert file paths to relative paths for the database.
        var analyzedImageRelativePath = Path.Combine(
            "uploads",
            "analyzed-frames",
            analyzedImageFileName);

        var jsonRelativePath = Path.Combine(
            "uploads",
            "analysis",
            jsonFileName);

        // 10. Save analysis results in the database.
        await SaveAnalysisToDatabaseAsync(
            frame,
            analysisResult,
            analyzedImageRelativePath,
            jsonRelativePath);

        // 11. Return the generated answer for Android TTS.
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
}