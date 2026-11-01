namespace VisionAid.Api.DTOs.Analysis;

public class OcrResultDto
{
    public string ExtractedText { get; set; } = null!;
    public float Confidence { get; set; }
}