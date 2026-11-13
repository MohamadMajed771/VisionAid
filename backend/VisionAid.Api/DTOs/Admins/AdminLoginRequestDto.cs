using System.ComponentModel.DataAnnotations;

namespace VisionAid.Api.DTOs.Admins;

public class AdminLoginRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;
}
