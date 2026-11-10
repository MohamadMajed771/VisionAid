using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisionAid.Api.DTOs.Admins;
using VisionAid.Api.Services.Interfaces;

namespace VisionAid.Api.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/admins")]
public class AdminsController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly ISessionService _sessionService;
    private readonly IFrameService _frameService;

    public AdminsController(
        IAdminService adminService,
        ISessionService sessionService,
        IFrameService frameService)
    {
        _adminService = adminService;
        _sessionService = sessionService;
        _frameService = frameService;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AdminAuthResponseDto>> Register(
        [FromBody] RegisterAdminRequestDto request)
    {
        try
        {
            var response = await _adminService.RegisterAsync(request);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AdminAuthResponseDto>> Login(
        [FromBody] AdminLoginRequestDto request)
    {
        try
        {
            return Ok(await _adminService.LoginAsync(request));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    [HttpGet("me")]
    public async Task<ActionResult<AdminResponseDto>> GetMe()
    {
        if (!TryGetAdminId(out var adminId))
            return Unauthorized();

        var admin = await _adminService.GetByIdAsync(adminId);
        return admin is null ? NotFound() : Ok(admin);
    }

    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyList<AdminUserResponseDto>>> GetUsers()
    {
        if (!TryGetAdminId(out var adminId))
            return Unauthorized();

        return Ok(await _adminService.GetUsersAsync(adminId));
    }

    [HttpPost("access-requests")]
    public async Task<ActionResult<AccessRequestResponseDto>> CreateAccessRequest(
        [FromBody] CreateAccessRequestDto request)
    {
        if (!TryGetAdminId(out var adminId))
            return Unauthorized();

        try
        {
            var response = await _adminService.CreateAccessRequestAsync(
                adminId,
                request);
            return Accepted(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpGet("access-requests")]
    public async Task<ActionResult<IReadOnlyList<AccessRequestResponseDto>>> GetAccessRequests()
    {
        if (!TryGetAdminId(out var adminId))
            return Unauthorized();

        return Ok(await _adminService.GetAccessRequestsAsync(adminId));
    }

    [HttpGet("users/{userId:int}/sessions")]
    public async Task<IActionResult> GetUserSessions(int userId)
    {
        if (!TryGetAdminId(out var adminId))
            return Unauthorized();

        var sessions = await _sessionService.GetSessionsForAdminAsync(
            adminId,
            userId);

        return sessions is null ? NotFound() : Ok(sessions);
    }

    [HttpGet("users/{userId:int}/sessions/{sessionId:int}/frames")]
    public async Task<IActionResult> GetUserSessionFrames(
        int userId,
        int sessionId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (!TryGetAdminId(out var adminId))
            return Unauthorized();

        try
        {
            var history = await _frameService.GetHistoryForAdminAsync(
                sessionId,
                userId,
                adminId,
                page,
                pageSize);

            return history is null ? NotFound() : Ok(history);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private bool TryGetAdminId(out int adminId)
    {
        return int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out adminId);
    }
}
