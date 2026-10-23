namespace VisionAid.Api.Entities;

public class UserPreference
{
    public long UserId { get; set; }

    public string PreferredLanguage { get; set; } = "en";

    public bool SpeechEnabled { get; set; } = true;

    public decimal SpeechSpeed { get; set; } = 1.0m;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}