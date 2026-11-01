using Microsoft.AspNetCore.Mvc;
using VisionAid.Api.DTOs.Frames;
using VisionAid.Api.Services.Interfaces;

namespace VisionAid.Api.Controllers;

[ApiController]
[Route("api/sessions/{sessionId:int}/frames")]
public class FramesController : ControllerBase
{
    private readonly IFrameService _frameService;
    private readonly IAnalysisService _analysisService;

    public FramesController(
        IFrameService frameService,
        IAnalysisService analysisService)
    {
        _frameService = frameService;
        _analysisService = analysisService;
    }

[HttpPost]
[Consumes("multipart/form-data")]
public async Task<IActionResult> UploadFrame(
    int sessionId,
    [FromForm] IFormFile image,
    [FromForm] string? question)
    {
        // 1. Save the original image and create its database record.
        var uploadedFrame = await _frameService.UploadFrameAsync(
            sessionId,
            image);

        // 2. Send the original image to FastAPI.
        // 3. Receive the ZIP and save the analysis results.
        var answer = await _analysisService.AnalyzeFrameAsync(
            uploadedFrame.FrameId,
            uploadedFrame.ImagePath,
            question);

        // 4. Return the result to Android.
        var response = new UploadAndAnalyzeFrameResponseDto
        {
            FrameId = uploadedFrame.FrameId,
            SessionId = uploadedFrame.SessionId,
            Answer = answer,
            CapturedAt = uploadedFrame.CapturedAt
        };

        return Ok(response);
    }
}