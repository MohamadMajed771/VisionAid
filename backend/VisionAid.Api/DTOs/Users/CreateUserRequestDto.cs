
using System.ComponentModel.DataAnnotations;

namespace VisionAid.Api.DTOs.Users;

public class CreateUserRequestDto
{
    [Required]
    [StringLength(150, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(10)]
    public string PreferredLanguage { get; set; } = "en";
}
