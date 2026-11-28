using Microsoft.AspNetCore.Http;

namespace VisionAid.Api.Services.Interfaces;

public interface IAnalysisService
{
    Task<string?> AnalyzeFrameAsync(
        int userId,
        IFormFile image,
        string language,
        IFormFile? audio);
}