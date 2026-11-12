
namespace VisionAid.Api.Models;

public class Admin
{
    public int AdminId { get; set; }

    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public ICollection<AdminAccessRequest> AccessRequests { get; set; }
        = new List<AdminAccessRequest>();
}
