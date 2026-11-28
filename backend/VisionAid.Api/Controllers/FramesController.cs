
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
[Route("api/analyse/frame")]
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
    [FromForm] UploadFrameRequestDto request)
{
    var userIdClaim = User.FindFirstValue(
        ClaimTypes.NameIdentifier);

    if (!int.TryParse(userIdClaim, out var userId))
        return Unauthorized();

    try
    {
        // 1. Get the user's preferred language.
        var preferredLanguage = await _context.Users
            .Where(u => u.UserId == userId)
            .Select(u => u.PreferredLanguage)
            .FirstOrDefaultAsync();

        // 2. Send the uploaded image directly for analysis.
        // Nothing is saved yet.
        var answer = await _analysisService.AnalyzeFrameAsync(
            userId,
            request.Image,
            preferredLanguage,
            request.Question);

        // 3. Python rejected the frame.
        // No frame or files were created.
        if (answer is null)
            return NoContent();

        // 4. Analysis was accepted and saved.
        return Ok(new
        {
            Answer = answer
        });
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
}
}
