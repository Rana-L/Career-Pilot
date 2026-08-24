namespace CareerPilot.Api.dto;

public class CvResponse
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
}
