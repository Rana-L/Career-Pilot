namespace CareerPilot.Api.Models;

public class CvAnalysis

{
    public int Id { get; set; }
    public int CvId { get; set; }
    public Cv Cv { get; set; } = null!;

    public int JobApplicationId { get; set; }
    public JobApplication JobApplication { get; set; } = null!;

    public int MatchScore { get; set; }
    public string? MissingSkills { get; set; }
    public Datetime CreatedAt { get; set; } = dateTime.UtcNow;


}