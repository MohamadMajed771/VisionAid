
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisionAid.Api.DTOs.Admins;
using VisionAid.Api.Services.Interfaces;

namespace VisionAid.Api.Controllers;

[Authorize(Roles = "User")]
[ApiController]
[Route("api/users/access-requests")]
public class UserAccessRequestsController : ControllerBase
{
    private readonly IAdminService _adminService;

    public UserAccessRequestsController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AccessRequestResponseDto>>> GetPending()
    {
        var userId = GetUserId();

        return Ok(await _adminService.GetPendingRequestsAsync(userId.Value));
    }

    [HttpPost("{requestId:int}/accept")]
    public Task<IActionResult> Accept(int requestId)
    {
        return Respond(requestId, accept: true);
    }

    [HttpPost("{requestId:int}/decline")]
    public Task<IActionResult> Decline(int requestId)
    {
        return Respond(requestId, accept: false);
    }

    [HttpDelete("admins/{adminId:int}")]
    public async Task<IActionResult> Revoke(int adminId)
    {
        var userId = GetUserId();

        var revoked = await _adminService.RevokeAdminAsync(userId.Value, adminId);
        return revoked ? NoContent() : NotFound();
    }

    private async Task<IActionResult> Respond(int requestId, bool accept)
    {
        var userId = GetUserId();

        var result = await _adminService.RespondToAccessRequestAsync(
            requestId,
            userId.Value,
            accept);

        if (result is null)
            return NotFound();

        return result.Value
            ? NoContent()
            : Conflict(new { message = "This access request has already been handled." });
    }

    private int? GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(value, out var userId))
            return null;

        return userId;
    }
}

