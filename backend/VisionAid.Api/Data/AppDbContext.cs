
using Microsoft.EntityFrameworkCore;
using VisionAid.Api.Models;

namespace VisionAid.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<StreamSession> StreamSessions => Set<StreamSession>();
    public DbSet<Frame> Frames => Set<Frame>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<AnalyzedFrame> AnalyzedFrames => Set<AnalyzedFrame>();
    public DbSet<Answer> Answers => Set<Answer>();
    public DbSet<DetectedObject> DetectedObjects => Set<DetectedObject>();
    public DbSet<OcrResult> OCRResults => Set<OcrResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // USERS
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("USERS");
            entity.HasKey(u => u.UserId);

            entity.Property(u => u.Name)
                .HasMaxLength(150);

            entity.Property(u => u.Email)
                .HasMaxLength(256);

            entity.HasIndex(u => u.Email)
                .IsUnique();

            entity.Property(u => u.PreferredLanguage)
                .HasMaxLength(10)
                .HasDefaultValue("en");
        });

        // STREAM_SESSIONS
        modelBuilder.Entity<StreamSession>(entity =>
        {
            entity.ToTable("STREAM_SESSIONS");
            entity.HasKey(s => s.SessionId);

            entity.Property(s => s.Status)
                .HasMaxLength(30)
                .HasDefaultValue("Active");

            entity.HasOne(s => s.User)
                .WithMany(u => u.StreamSessions)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // FRAMES: original captured images and global depth information
        modelBuilder.Entity<Frame>(entity =>
        {
            entity.ToTable("FRAMES");
            entity.HasKey(f => f.FrameId);

            entity.Property(f => f.ImagePath)
                .HasMaxLength(1024);

            entity.HasOne(f => f.Session)
                .WithMany(s => s.Frames)
                .HasForeignKey(f => f.SessionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(f => new { f.SessionId, f.CapturedAt });
        });

        // QUESTIONS
        modelBuilder.Entity<Question>(entity =>
        {
            entity.ToTable("QUESTIONS");
            entity.HasKey(q => q.QuestionId);

            entity.Property(q => q.QuestionText)
                .HasMaxLength(4000);

            entity.HasOne(q => q.Session)
                .WithMany(s => s.Questions)
                .HasForeignKey(q => q.SessionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ANALYZED_FRAMES: AI analysis results for an original frame
        modelBuilder.Entity<AnalyzedFrame>(entity =>
        {
            entity.ToTable("ANALYZED_FRAMES");
            entity.HasKey(a => a.AnalyzedFrameId);

            entity.Property(a => a.AnalyzedImagePath)
                .HasMaxLength(1024);

            entity.Property(a => a.JsonFilePath)
                .HasMaxLength(1024);

            entity.HasOne(a => a.Frame)
                .WithMany(f => f.AnalyzedFrames)
                .HasForeignKey(a => a.FrameId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(a => new { a.FrameId, a.AnalyzedAt });
        });

        // ANSWERS: one answer per question, linked to an analyzed frame
        modelBuilder.Entity<Answer>(entity =>
        {
            entity.ToTable("ANSWERS");
            entity.HasKey(a => a.AnswerId);

            entity.Property(a => a.AnswerText)
                .HasMaxLength(8000);

            // One question can have at most one answer.
            entity.HasOne(a => a.Question)
                .WithOne(q => q.Answer)
                .HasForeignKey<Answer>(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            // An analyzed frame may support multiple answers.
            entity.HasOne(a => a.AnalyzedFrame)
                .WithMany(f => f.Answers)
                .HasForeignKey(a => a.AnalyzedFrameId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(a => a.QuestionId)
                .IsUnique();
        });

        // DETECTED_OBJECTS
        modelBuilder.Entity<DetectedObject>(entity =>
        {
            entity.ToTable("DETECTED_OBJECTS");
            entity.HasKey(d => d.DetectedObjectId);

            entity.Property(d => d.Label)
                .HasMaxLength(200);

            entity.HasOne(d => d.AnalyzedFrame)
                .WithMany(a => a.DetectedObjects)
                .HasForeignKey(d => d.AnalyzedFrameId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // OCR_RESULTS
        modelBuilder.Entity<OcrResult>(entity =>
        {
            entity.ToTable("OCR_RESULTS");
            entity.HasKey(o => o.OCRResultId);

            entity.Property(o => o.ExtractedText)
                .HasMaxLength(8000);

            entity.HasOne(o => o.AnalyzedFrame)
                .WithMany(a => a.OcrResults)
                .HasForeignKey(o => o.AnalyzedFrameId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
