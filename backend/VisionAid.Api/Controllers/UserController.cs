
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisionAid.Api.DTOs.Users;
using VisionAid.Api.Services.Interfaces;

namespace VisionAid.Api.Controllers;
[Authorize(Roles = "User")]
[ApiController]

[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserResponseDto>> GetById(int id)
    {
        if (!int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var authenticatedUserId))
        {
            return Unauthorized();
        }

        if (authenticatedUserId != id)
            return Forbid();

        var user = await _userService.GetByIdAsync(id);

        if (user is null)
            return NotFound();

        return Ok(user);
    }
}
