using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisionAid.Api.DTOs.Sessions;
using VisionAid.Api.Services.Interfaces;

namespace VisionAid.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SessionsController : ControllerBase
{
    private readonly ISessionService _sessionService;

    public SessionsController(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }

    private int? GetAuthenticatedUserId()
    {
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier
        );

        if (!int.TryParse(userIdClaim, out var userId))
            return null;

        return userId;
    }

    
[HttpPost]
public async Task<ActionResult<SessionResponseDto>> Create()
{
    var userId = GetAuthenticatedUserId();

    if (userId == null)
        return Unauthorized();

    try
    {
        var session = await _sessionService.CreateAsync(userId.Value);

        if (session == null)
            return NotFound("User was not found.");

        return Ok(session);
    }
    catch (InvalidOperationException ex)
    {
        return Conflict(ex.Message);
    }
}


    [HttpGet("active")]
    public async Task<ActionResult<SessionResponseDto>> GetActive()
    {
        var userId = GetAuthenticatedUserId();

        if (userId == null)
            return Unauthorized();

        var session = await _sessionService.GetActiveAsync(userId.Value);

        if (session == null)
            return NotFound("No active session was found.");

        return Ok(session);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SessionResponseDto>> GetById(int id)
    {
        var userId = GetAuthenticatedUserId();

        if (userId == null)
            return Unauthorized();

        var session = await _sessionService.GetByIdAsync(
            id,
            userId.Value
        );

        if (session == null)
            return NotFound("Session was not found.");

        return Ok(session);
    }

    [HttpPost("{id:int}/end")]
    public async Task<IActionResult> End(int id)
    {
        var userId = GetAuthenticatedUserId();

        if (userId == null)
            return Unauthorized();

        var ended = await _sessionService.EndAsync(
            id,
            userId.Value
        );

        if (!ended)
            return NotFound("Session was not found.");

        return NoContent();
    }
}