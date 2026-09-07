namespace CareerPilot.Api.dto;

public class CvAnalysisResponse
{
    public int Id { get; set; }
    public int CvId { get; set; }
    public int JobApplicationId { get; set; }
    public int MatchScore { get; set; }
    public string? MissingSkills { get; set; }
    public DateTime CreatedAt { get; set; }
}
