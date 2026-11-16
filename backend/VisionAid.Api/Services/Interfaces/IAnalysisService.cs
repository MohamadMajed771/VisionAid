namespace VisionAid.Api.Services.Interfaces;

public interface IAnalysisService
{
Task<string?> AnalyzeFrameAsync(
    int frameId,
    string originalImagePath,
    IFormFile? audio = null);
}