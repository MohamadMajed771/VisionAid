namespace VisionAid.Api.Entities;

public class AnswerFrame
{
    public long AnswerId { get; set; }

    public long FrameId { get; set; }

    public double? RelevanceScore { get; set; }

    public Answer Answer { get; set; } = null!;

    public Frame Frame { get; set; } = null!;
}