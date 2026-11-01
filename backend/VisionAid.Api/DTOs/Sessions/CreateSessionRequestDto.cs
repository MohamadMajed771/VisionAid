
using System.ComponentModel.DataAnnotations;

namespace VisionAid.Api.DTOs.Sessions;

public class CreateSessionRequestDto
{
    [Required]
    [Range(1, int.MaxValue)]
    public int UserId { get; set; }
}
