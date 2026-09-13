using System.Text;
using System.Text.Json;
using CareerPilot.Api.dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPilot.Api.Controllers;

[ApiController]
[Route("api/job-search")]
[Authorize]
[EnableRateLimiting("jobsearch")]
public class JobSearchController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public JobSearchController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<ActionResult<List<JobSearchResult>>> Search(
        [FromQuery] string title,
        [FromQuery] string location,
        [FromQuery] int radiusMiles = 10,
        [FromQuery] int page = 1)
    {
        if (string.IsNullOrWhiteSpace(title))
            return BadRequest("Enter a job title to search for.");
        if (string.IsNullOrWhiteSpace(location))
            return BadRequest("Enter a location to search near.");

        var appId = _configuration["Adzuna:AppId"];
        var appKey = _configuration["Adzuna:AppKey"];
        var radiusKm = (int)Math.Round(radiusMiles * 1.60934);

        var url = "https://api.adzuna.com/v1/api/jobs/gb/search/" + page
            + $"?app_id={Uri.EscapeDataString(appId!)}"
            + $"&app_key={Uri.EscapeDataString(appKey!)}"
            + $"&results_per_page=20"
            + $"&what={Uri.EscapeDataString(title)}"
            + $"&where={Uri.EscapeDataString(location)}"
            + $"&distance={radiusKm}"
            + $"&content-type=application/json";

        var client = _httpClientFactory.CreateClient();
        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync(url);
        }
        catch (Exception)
        {
            return StatusCode(502, "Couldn't reach the job search provider. Try again shortly.");
        }

        if (!response.IsSuccessStatusCode)
            return StatusCode(502, "The job search provider returned an error. Try again shortly.");

        var bodyBytes = await response.Content.ReadAsByteArrayAsync();
        var body = Encoding.UTF8.GetString(bodyBytes);
        var parsed = JsonSerializer.Deserialize<AdzunaSearchResponse>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        var results = parsed.Results.Select(job => new JobSearchResult
        {
            Title = job.Title,
            CompanyName = job.Company.DisplayName,
            Location = job.Location.DisplayName,
            Description = job.Description,
            Url = job.RedirectUrl,
            Created = job.Created,
        }).ToList();

        return Ok(results);
    }
}
