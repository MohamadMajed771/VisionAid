
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using VisionAid.Api.DTOs.Frames;
using VisionAid.Api.Services.Interfaces;
using VisionAid.Api.Data;
namespace VisionAid.Api.Controllers;

[Authorize(Roles = "User")]
[ApiController]
[Route("api/sessions/{sessionId:int}/frames")]
public class FramesController : ControllerBase
{
    private readonly IFrameService _frameService;
    private readonly IAnalysisService _analysisService;
private readonly AppDbContext _context;
    public FramesController(
        IFrameService frameService,
        IAnalysisService analysisService, AppDbContext context)
    {
        _frameService = frameService;
        _analysisService = analysisService;
_context=context;
    }
[HttpPost]
[Consumes("multipart/form-data")]
public async Task<IActionResult> UploadFrame(
    int sessionId,
    [FromForm] UploadFrameRequestDto request)
{
    var userIdClaim = User.FindFirstValue(
        ClaimTypes.NameIdentifier);

    if (!int.TryParse(userIdClaim, out var userId))
        return Unauthorized();

    try
    {
     var preferredLanguage = await _context.Users
    .Where(u => u.UserId == userId)
    .Select(u => u.PreferredLanguage)
    .FirstOrDefaultAsync();
        // Save the original image and create its database record.
        var uploadedFrame = await _frameService.UploadFrameAsync(
            sessionId,
            userId,
            request.Image);

        // Send the image, optional audio, and preferred language for analysis.
        var answer = await _analysisService.AnalyzeFrameAsync(
            uploadedFrame.FrameId,
            uploadedFrame.ImagePath,
             preferredLanguage,
            request.Question
           );

        var response = new UploadAndAnalyzeFrameResponseDto
        {
            FrameId = uploadedFrame.FrameId,
            SessionId = uploadedFrame.SessionId,
            Answer = answer,
            CapturedAt = uploadedFrame.CapturedAt
        };

        return Ok(response);
    }
    catch (KeyNotFoundException ex)
    {
        return NotFound(ex.Message);
    }
    catch (InvalidOperationException ex)
    {
        return Conflict(ex.Message);
    }
    catch (ArgumentException ex)
    {
        return BadRequest(ex.Message);
    }
}}
