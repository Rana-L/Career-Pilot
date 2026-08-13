namespace CareerPilot.Api.models;

public class CvAnalysis

{
    public int Id { get; set; }
    public int CvId { get; set; }
    public Cv Cv { get; set; } = null!;

    public int JobApplicationId { get; set; }
    public JobApplication JobApplication { get; set; } = null!;

    public int MatchScore { get; set; }
    public string? MissingSkills { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


}