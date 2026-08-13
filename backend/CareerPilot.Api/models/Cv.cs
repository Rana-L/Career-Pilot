namespace CareerPilot.Api.models;

public class Cv

{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string FileName { get; set; } = string.Empty;
    public string S3Url { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CvAnalysis> CvAnalyses { get; set; } = new List<CvAnalysis>();

}