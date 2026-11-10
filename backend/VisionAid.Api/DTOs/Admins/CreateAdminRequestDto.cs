using System.ComponentModel.DataAnnotations;

namespace VisionAid.Api.DTOs.Admins;

/// <summary>Payload for creating an admin.</summary>
public class CreateAdminRequestDto
{
    /// <summary>The admin's display name.</summary>
    [Required, StringLength(150, MinimumLength = 1)]
    public string Name { get; set; } = null!;

    /// <summary>The admin's email address.</summary>
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = null!;
}