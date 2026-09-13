namespace CareerPilot.Api.models;

// One saved search per user — remembers what they last searched for so the
// job search page can restore it (and, later, so we can check for new
// matching jobs on a schedule).
public class SavedJobSearch
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int RadiusMiles { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
