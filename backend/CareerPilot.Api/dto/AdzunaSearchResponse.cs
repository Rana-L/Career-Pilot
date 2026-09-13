using System.Text.Json.Serialization;

namespace CareerPilot.Api.dto;

public class AdzunaSearchResponse
{
    public List<AdzunaJob> Results { get; set; } = new();
}

public class AdzunaJob
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Created { get; set; }

    [JsonPropertyName("redirect_url")]
    public string RedirectUrl { get; set; } = string.Empty;

    public AdzunaCompany Company { get; set; } = new();
    public AdzunaLocation Location { get; set; } = new();
}

public class AdzunaCompany
{
    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;
}

public class AdzunaLocation
{
    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;
}
