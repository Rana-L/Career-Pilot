namespace CareerPilot.Api.Models;

public class Cv

{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string FileName { get; set; } = string.Empty;
    public string S3Url { get; set; } = string.Empty;
    public DateTime UploadAt { get; set; } = DateTime.UtcvNow;

    public ICollection<CvAnalysis> CvAnlyses { get; set; } = new List<CvAnalysis>();

}