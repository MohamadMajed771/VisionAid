namespace VisionAid.Api.Models;

public class Admin
{
    public int AdminId { get; set; }

    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public ICollection<User> Users { get; set; }
        = new List<User>();
}
