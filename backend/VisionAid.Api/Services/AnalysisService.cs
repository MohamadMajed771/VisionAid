using System.Net.Http.Headers;
using Microsoft.AspNetCore.StaticFiles;
using VisionAid.Api.Services.Interfaces;

namespace VisionAid.Api.Services;

public class AnalysisService : IAnalysisService
{
    private readonly HttpClient _httpClient;
    private readonly IFrameService _frameService;
    private readonly IWebHostEnvironment _environment;

    public AnalysisService(
        HttpClient httpClient,
        IFrameService frameService,
        IWebHostEnvironment environment)
    {
        _httpClient = httpClient;
        _frameService = frameService;
        _environment = environment;
    }

    public async Task<string?> AnalyzeFrameAsync(
        int frameId,
        string originalImagePath, string? question = null)
    {
        // 1. Resolve the original image's full path.
        var backendFolder = Path.GetFullPath(
            Path.Combine(_environment.ContentRootPath, ".."));

        var normalizedPath = originalImagePath
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);

        var fullImagePath = Path.GetFullPath(
            Path.Combine(backendFolder, normalizedPath));

        if (!File.Exists(fullImagePath))
            throw new FileNotFoundException(
                "Original image was not found.",
                fullImagePath);

        // 2. Open the original image.
        await using var imageStream = new FileStream(
            fullImagePath,
            FileMode.Open,
            FileAccess.Read);

        // 3. Prepare the multipart/form-data request.
        using var form = new MultipartFormDataContent();

        var imageContent = new StreamContent(imageStream);

        // Determine the image's content type from its extension.
        var contentTypeProvider = new FileExtensionContentTypeProvider();

        if (!contentTypeProvider.TryGetContentType(
            fullImagePath,
            out var contentType))
        {
            contentType = "application/octet-stream";
        }

        imageContent.Headers.ContentType =
            new MediaTypeHeaderValue(contentType);

        form.Add(
            imageContent,
            "image",
            Path.GetFileName(fullImagePath));
            if (!string.IsNullOrWhiteSpace(question))
             {
         form.Add(
        new StringContent(question),
        "question");
            }

        // 4. Send the image to FastAPI.
        using var response = await _httpClient.PostAsync(
            "analyze",
            form);

        // Throw an exception if FastAPI returns an unsuccessful status.
        response.EnsureSuccessStatusCode();

        // 5. Receive the ZIP response.
        await using var zipStream =
            await response.Content.ReadAsStreamAsync();

        // 6. Pass the ZIP to FrameService.
        return await _frameService.ProcessAnalysisResultAsync(
            frameId,
            zipStream);
    }
}