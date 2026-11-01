
using Microsoft.AspNetCore.Mvc;
using VisionAid.Api.DTOs.Users;
using VisionAid.Api.Services.Interfaces;

namespace VisionAid.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost]
    public async Task<ActionResult<UserResponseDto>> Create(
        [FromBody] CreateUserRequestDto request)
    {
        var user = await _userService.CreateAsync(request);

        if (user is null)
        {
            return Conflict(new
            {
                message = "A user with this email already exists."
            });
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = user.UserId },
            user);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserResponseDto>> GetById(int id)
    {
        var user = await _userService.GetByIdAsync(id);

        if (user is null)
            return NotFound();

        return Ok(user);
    }
}
