
using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.StaticFiles;
using VisionAid.Api.Services.Interfaces;

namespace VisionAid.Api.Services;

public class AnalysisService : IAnalysisService
{
    private readonly HttpClient _httpClient;
    private readonly IFrameService _frameService;

    public AnalysisService(
        HttpClient httpClient,
        IFrameService frameService)
    {
        _httpClient = httpClient;
        _frameService = frameService;
    }

    public async Task<string?> AnalyzeFrameAsync(
        int userId,
        IFormFile image,
        string language,
        IFormFile? audio)
    {
        // 1. Validate the upload and find the user's active session.
        // Nothing is saved to disk or database at this point.
        var sessionId = await _frameService.ValidateFrameUploadAsync(
            userId,
            image);

        // Capture the time before AI analysis begins.
        var capturedAt = DateTime.UtcNow;

        // 2. Open the uploaded image directly from memory/request storage.
        await using var imageStream = image.OpenReadStream();

        // 3. Prepare multipart/form-data.
        using var form = new MultipartFormDataContent();

        form.Add(
            new StringContent(language),
            "language");

        // Add image.
        var imageContent = new StreamContent(imageStream);

        var contentTypeProvider =
            new FileExtensionContentTypeProvider();

        if (!contentTypeProvider.TryGetContentType(
            image.FileName,
            out var contentType))
        {
            contentType = "application/octet-stream";
        }

        imageContent.Headers.ContentType =
            new MediaTypeHeaderValue(contentType);

        form.Add(
            imageContent,
            "image",
            image.FileName);

        // 4. Add optional audio.
        if (audio is not null && audio.Length > 0)
        {
            var audioStream = audio.OpenReadStream();
            var audioContent = new StreamContent(audioStream);

            audioContent.Headers.ContentType =
                new MediaTypeHeaderValue("audio/wav");

            form.Add(
                audioContent,
                "audio",
                audio.FileName);
        }

        // 5. Send the request to Python.
        using var response = await _httpClient.PostAsync(
            "analyze",
            form);

        // 6. Python rejected the frame (e.g. blurry image,
        //    or no required question).
        // Save absolutely nothing.
        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return null;
        }

        // Throw for other unsuccessful responses.
        response.EnsureSuccessStatusCode();

        // 7. Successful response: receive ZIP.
        await using var zipStream =
            await response.Content.ReadAsStreamAsync();

        // 8. FrameService now saves the original image,
        //    annotated image, JSON, and database records.
        return await _frameService.ProcessAnalysisResultAsync(
            sessionId,
            image,
            capturedAt,
            zipStream);
    }
}
