
using Microsoft.EntityFrameworkCore;
using VisionAid.Api.Entities;

namespace VisionAid.Api.Data;

public class VisionAidDbContext : DbContext
{
    public VisionAidDbContext(DbContextOptions<VisionAidDbContext> options)
        : base(options)
    {
    }

    // Database tables
    public DbSet<User> Users => Set<User>();
    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();
    public DbSet<StreamSession> StreamSessions => Set<StreamSession>();
    public DbSet<Frame> Frames => Set<Frame>();
    public DbSet<OcrResult> OcrResults => Set<OcrResult>();
    public DbSet<DetectedObject> DetectedObjects => Set<DetectedObject>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Answer> Answers => Set<Answer>();
    public DbSet<AnswerFrame> AnswerFrames => Set<AnswerFrame>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // =========================
        // User
        // =========================
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);

            entity.Property(u => u.FullName)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(254);

            entity.HasIndex(u => u.Email)
                .IsUnique();

            entity.Property(u => u.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
        });

        // =========================
        // UserPreference
        // One User -> Zero or One Preference
        // =========================
        modelBuilder.Entity<UserPreference>(entity =>
        {
            entity.HasKey(p => p.UserId);

            entity.Property(p => p.PreferredLanguage)
                .IsRequired()
                .HasMaxLength(5)
                .HasDefaultValue("en");

            entity.Property(p => p.SpeechSpeed)
                .HasDefaultValue(1.0m);

            entity.Property(p => p.SpeechEnabled)
                .HasDefaultValue(true);

        
            entity.HasOne(p => p.User)
                .WithOne(u => u.Preferences)
                .HasForeignKey<UserPreference>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // =========================
        // StreamSession
        // One User -> Many Sessions
        // =========================
        modelBuilder.Entity<StreamSession>(entity =>
        {
            entity.HasKey(s => s.Id);

            entity.Property(s => s.ClientType)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(s => s.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("Active");

            entity.HasIndex(s => new { s.UserId, s.StartedAt });

            entity.HasOne(s => s.User)
                .WithMany(u => u.StreamSessions)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // =========================
        // Frame
        // One Session -> Many Frames
        // =========================
        modelBuilder.Entity<Frame>(entity =>
        {
            entity.HasKey(f => f.Id);

            entity.Property(f => f.ImagePath)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(f => f.SharpnessScore);

            entity.HasIndex(f => new { f.SessionId, f.CapturedAt });

            entity.HasOne(f => f.Session)
                .WithMany(s => s.Frames)
                .HasForeignKey(f => f.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // =========================
        // OcrResult
        // One Frame -> Many OCR Results
        // =========================
        modelBuilder.Entity<OcrResult>(entity =>
        {
            entity.HasKey(o => o.Id);

            entity.Property(o => o.ExtractedText)
                .IsRequired()
                .HasColumnType("longtext");

            entity.Property(o => o.BoundingBox)
                .HasColumnType("json");

            entity.HasIndex(o => o.FrameId);

            entity.HasOne(o => o.Frame)
                .WithMany(f => f.OcrResults)
                .HasForeignKey(o => o.FrameId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // =========================
        // DetectedObject
        // One Frame -> Many Detected Objects
        // =========================
        modelBuilder.Entity<DetectedObject>(entity =>
        {
            entity.HasKey(d => d.Id);

            entity.Property(d => d.Label)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(d => d.BoundingBox)
                .IsRequired()
                .HasColumnType("json");

            entity.HasIndex(d => new { d.FrameId, d.Label });

            entity.HasOne(d => d.Frame)
                .WithMany(f => f.DetectedObjects)
                .HasForeignKey(d => d.FrameId)
                .OnDelete(DeleteBehavior.Cascade);
                entity.Property(d => d.DepthEstimate);

            entity.Property(d => d.DepthUnit)
                 .HasMaxLength(20);
        });

        // =========================
        // Question
        // One Session -> Many Questions
        // =========================
        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(q => q.Id);

            entity.Property(q => q.QuestionText)
                .IsRequired()
                .HasColumnType("longtext");

            entity.HasIndex(q => new { q.SessionId, q.AskedAt });

            entity.HasOne(q => q.Session)
                .WithMany(s => s.Questions)
                .HasForeignKey(q => q.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // =========================
        // Answer
        // One Question -> Zero or One Answer
        // =========================
        modelBuilder.Entity<Answer>(entity =>
        {
            entity.HasKey(a => a.Id);

            entity.Property(a => a.AnswerText)
                .IsRequired()
                .HasColumnType("longtext");

            entity.Property(a => a.Language)
                .IsRequired()
                .HasMaxLength(5);

            entity.Property(a => a.ConfidenceStatus)
                .IsRequired()
                .HasMaxLength(30)
                .HasDefaultValue("Uncertain");

            entity.HasIndex(a => a.QuestionId)
                .IsUnique();

            entity.HasOne(a => a.Question)
                .WithOne(q => q.Answer)
                .HasForeignKey<Answer>(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // =========================
        // AnswerFrame
        // Many-to-many: Answers <-> Frames
        // =========================
        modelBuilder.Entity<AnswerFrame>(entity =>
        {
            entity.HasKey(af => new { af.AnswerId, af.FrameId });

            entity.HasOne(af => af.Answer)
                .WithMany(a => a.AnswerFrames)
                .HasForeignKey(af => af.AnswerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(af => af.Frame)
                .WithMany(f => f.AnswerFrames)
                .HasForeignKey(af => af.FrameId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
