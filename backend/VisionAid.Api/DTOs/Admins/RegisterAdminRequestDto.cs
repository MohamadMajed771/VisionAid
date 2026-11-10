using System.ComponentModel.DataAnnotations;

namespace VisionAid.Api.DTOs.Admins;

public class RegisterAdminRequestDto
{
    [Required]
    [StringLength(150, MinimumLength = 1)]
    public string Name { get; set; } = null!;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = null!;

    [Required]
    [StringLength(128, MinimumLength = 8)]
    public string Password { get; set; } = null!;
}
