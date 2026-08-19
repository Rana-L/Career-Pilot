using CareerPilot.Api.models;

namespace CareerPilot.Api.dto;

public class UpdateApplicationRequest
{
    public string CompanyName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string? JobDescription { get; set; }
    public ApplicationStatus Status { get; set; }
    public string? Notes { get; set; }  
}