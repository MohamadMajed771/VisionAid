public class DetectedObjectDto
{
    public string Label { get; set; } = null!;

    public float Confidence { get; set; }
    public float Depth { get; set; }
    public string? DepthUnit { get; set; }

    public float X1 { get; set; }
    public float Y1 { get; set; }
    public float X2 { get; set; }
    public float Y2 { get; set; }
}