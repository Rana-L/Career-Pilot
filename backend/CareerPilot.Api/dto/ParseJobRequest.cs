namespace CareerPilot.Api.dto;

public class ParseJobRequest
{
    // Either the raw job posting text, or a URL to it — the backend detects which.
    public string Text { get; set; } = string.Empty;
}
