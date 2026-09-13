using System.Text;
using System.Text.Json;
using CareerPilot.Api.data;
using CareerPilot.Api.dto;
using CareerPilot.Api.models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CareerPilot.Api.Controllers;

[ApiController]
[Route("api/job-search")]
[Authorize]
[EnableRateLimiting("jobsearch")]
public class JobSearchController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _context;

    public JobSearchController(IHttpClientFactory httpClientFactory, IConfiguration configuration, AppDbContext context)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _context = context;
    }

    private int GetUserId()
    {
        return int.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
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

    [HttpGet("saved")]
    public async Task<ActionResult<SavedJobSearchResponse>> GetSaved()
    {
        var userId = GetUserId();

        var saved = await _context.SavedJobSearches
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (saved is null)
        {
            return NoContent();
        }

        return Ok(new SavedJobSearchResponse
        {
            Title = saved.Title,
            Location = saved.Location,
            RadiusMiles = saved.RadiusMiles,
        });
    }

    [HttpPut("saved")]
    public async Task<IActionResult> SaveSearch(SavedJobSearchRequest request)
    {
        var userId = GetUserId();

        var saved = await _context.SavedJobSearches
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (saved is null)
        {
            saved = new SavedJobSearch { UserId = userId };
            _context.SavedJobSearches.Add(saved);
        }

        saved.Title = request.Title;
        saved.Location = request.Location;
        saved.RadiusMiles = request.RadiusMiles;
        saved.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }
}
