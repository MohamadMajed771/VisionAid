namespace VisionAid.Api.Entities;

public class User
{
    public long Id { get; set; }

    public required string FullName { get; set; }

    public required string Email { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public UserPreference? Preferences { get; set; }

    public ICollection<StreamSession> StreamSessions { get; set; }
        = new List<StreamSession>();
}