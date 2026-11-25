namespace VisionAid.Api.Services.Interfaces;

public interface IAnalysisService
{
Task<string?> AnalyzeFrameAsync(
    int frameId,
    string originalImagePath,
    string language,
    IFormFile? audio = null
    );
}