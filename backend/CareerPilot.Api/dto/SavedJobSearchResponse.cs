namespace CareerPilot.Api.dto;

public class SavedJobSearchResponse
{
    public string Title { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int RadiusMiles { get; set; }
}
