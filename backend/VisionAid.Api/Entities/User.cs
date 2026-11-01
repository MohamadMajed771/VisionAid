namespace VisionAid.Api.Models;

public class User
{
    public int UserId { get; set; }

    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;

    // Language used for questions and spoken answers.
    public string PreferredLanguage { get; set; } = "en";

    public DateTime CreatedAt { get; set; }

    public ICollection<StreamSession> StreamSessions { get; set; }
        = new List<StreamSession>();
}