namespace CareerPilot.Api.dto;

public class DocumentRequest
{
    public string Markdown { get; set; } = string.Empty;
    public string? FileName { get; set; }
}
