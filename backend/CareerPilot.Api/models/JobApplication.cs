namespace CareerPilot.Api.Models;

public class JobApplication

{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string CompanyName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string? JibDescription { get; set; }
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Wishlist;
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CvAnalysis> CvAnalyses { get; set; } = new List<CvAnalysis>();
}