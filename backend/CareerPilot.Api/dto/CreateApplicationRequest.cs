namespace CareerPilot.Api.dto;

public class CreateApplicationRequest
{
    public string CompanyName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string? JobDescription { get; set; }
    public string? Notes { get; set; }
}