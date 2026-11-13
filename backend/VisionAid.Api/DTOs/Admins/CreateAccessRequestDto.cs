using System.ComponentModel.DataAnnotations;

namespace VisionAid.Api.DTOs.Admins;

public class CreateAccessRequestDto
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string UserEmail { get; set; } = null!;
}
