using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisionAid.Api.Services;

namespace VisionAid.Api.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/admins/users/{userId:int}/sessions/{sessionId:int}/frames/{frameId:int}")]
[ResponseCache(
    NoStore = true,
    Location = ResponseCacheLocation.None)]
public class AdminFrameImagesController : ControllerBase
{
    private readonly MonitoringImageService _images;

    public AdminFrameImagesController(
        MonitoringImageService images)
    {
        _images = images;
    }

    [HttpGet("image")]
    public Task<IActionResult> Original(
        int userId,
        int sessionId,
        int frameId)
    {
        return GetImage(
            userId,
            sessionId,
            frameId,
            false);
    }

    [HttpGet("analyzed-image")]
    public Task<IActionResult> Analyzed(
        int userId,
        int sessionId,
        int frameId)
    {
        return GetImage(
            userId,
            sessionId,
            frameId,
            true);
    }

    private async Task<IActionResult> GetImage(
        int userId,
        int sessionId,
        int frameId,
        bool analyzed)
    {
        var claim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(claim, out var adminId))
            return Unauthorized();

        var image = await _images.GetAsync(
            adminId,
            userId,
            sessionId,
            frameId,
            analyzed);

        if (image is null)
            return NotFound();

        Response.Headers["X-Content-Type-Options"] =
            "nosniff";

        return PhysicalFile(
            image.FullPath,
            image.ContentType);
    }
}