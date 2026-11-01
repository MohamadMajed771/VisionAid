
using Microsoft.AspNetCore.Mvc;
using VisionAid.Api.DTOs.Sessions;
using VisionAid.Api.Services.Interfaces;

namespace VisionAid.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SessionsController : ControllerBase
{
    private readonly ISessionService _sessionService;

    public SessionsController(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }

    [HttpPost]
    public async Task<ActionResult<SessionResponseDto>> Create(
        [FromBody] CreateSessionRequestDto request)
    {
        var session = await _sessionService.CreateAsync(request);

        if (session is null)
            return NotFound("User was not found.");

        return CreatedAtAction(
            nameof(GetById),
            new { id = session.SessionId },
            session);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SessionResponseDto>> GetById(int id)
    {
        var session = await _sessionService.GetByIdAsync(id);

        if (session is null)
            return NotFound("Session was not found.");

        return Ok(session);
    }

    [HttpPost("{id:int}/end")]
    public async Task<IActionResult> End(int id)
    {
        var ended = await _sessionService.EndAsync(id);

        if (!ended)
            return NotFound("Session was not found.");

        return NoContent();
    }
}
