using CareerPilot.Api.data;
using CareerPilot.Api.dto;
using CareerPilot.Api.models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenAI.Chat;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;


namespace CareerPilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApplicationsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ChatClient _chatClient;
    private readonly IHttpClientFactory _httpClientFactory;

    public ApplicationsController(AppDbContext context, ChatClient chatClient, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _chatClient = chatClient;
        _httpClientFactory = httpClientFactory;
    }

    private int GetUserId()
    {
        return int.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
    }

    [HttpPost("parse")]
    public async Task<ActionResult<ParseJobResponse>> Parse(ParseJobRequest request)
    {
        var input = request.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(input))
            return BadRequest("Paste a job posting or a URL first.");

        string postingText;

        var looksLikeUrl = Uri.TryCreate(input, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            && !input.Contains(' ') && !input.Contains('\n');

        if (looksLikeUrl)
        {
            var (fetched, error) = await FetchJobPostingTextAsync(uri!);
            if (error != null) return BadRequest(error);
            postingText = fetched!;
        }
        else
        {
            postingText = input;
        }

        var prompt = $@"Extract the company name, job title, and job description from the job posting below.
Respond ONLY with a JSON object in this exact shape:
{{""companyName"": ""..."", ""jobTitle"": ""..."", ""jobDescription"": ""...""}}
For jobDescription, include the responsibilities and requirements as clean plain text; leave out
boilerplate like equal-opportunity statements, benefits fluff, and application instructions. If a field
can't be found, use an empty string.

Job posting:
{postingText}";

        var chatResponse = await _chatClient.CompleteChatAsync(
            [new UserChatMessage(prompt)],
            new ChatCompletionOptions { ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat() });

        var json = chatResponse.Value.Content[0].Text;
        var parsed = JsonSerializer.Deserialize<ParseJobResponse>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        return Ok(parsed);
    }

    private async Task<(string? Text, string? Error)> FetchJobPostingTextAsync(Uri uri)
    {
        if (!await IsSafeExternalHostAsync(uri))
            return (null, "That URL can't be fetched.");

        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(10);
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36");

        string html;
        try
        {
            var response = await client.GetAsync(uri);
            if (!response.IsSuccessStatusCode)
                return (null, $"Couldn't fetch that page (HTTP {(int)response.StatusCode}). Some sites (LinkedIn, Indeed) block automated requests — try pasting the job description text instead.");
            html = await response.Content.ReadAsStringAsync();
        }
        catch (Exception)
        {
            return (null, "Couldn't reach that URL. Try pasting the job description text instead.");
        }

        var text = StripHtml(html);
        if (text.Length < 200)
            return (null, "That page didn't return readable content automatically — common for LinkedIn, Indeed, and other JS-heavy job boards. Copy and paste the job description text instead.");

        if (text.Length > 8000) text = text[..8000];

        return (text, null);
    }

    private static string StripHtml(string html)
    {
        var noScripts = Regex.Replace(html, @"<script[\s\S]*?</script>", " ", RegexOptions.IgnoreCase);
        var noStyles = Regex.Replace(noScripts, @"<style[\s\S]*?</style>", " ", RegexOptions.IgnoreCase);
        var withBreaks = Regex.Replace(noStyles, @"</(p|div|li|br|h[1-6]|tr)>", "\n", RegexOptions.IgnoreCase);
        var noTags = Regex.Replace(withBreaks, "<[^>]+>", " ");
        var decoded = WebUtility.HtmlDecode(noTags);
        var collapsed = Regex.Replace(decoded, @"[ \t]+", " ");
        collapsed = Regex.Replace(collapsed, @"\n\s*\n+", "\n\n");
        return collapsed.Trim();
    }

    // Blocks the server from fetching internal/private network addresses on the user's behalf
    // (SSRF protection) — e.g. cloud metadata endpoints or internal services.
    private static async Task<bool> IsSafeExternalHostAsync(Uri uri)
    {
        try
        {
            var addresses = await Dns.GetHostAddressesAsync(uri.Host);
            return addresses.Length > 0 && addresses.All(IsPublicAddress);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsPublicAddress(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip)) return false;
        var bytes = ip.GetAddressBytes();
        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            if (bytes[0] == 10) return false;
            if (bytes[0] == 172 && bytes[1] is >= 16 and <= 31) return false;
            if (bytes[0] == 192 && bytes[1] == 168) return false;
            if (bytes[0] == 169 && bytes[1] == 254) return false;
            if (bytes[0] == 0) return false;
        }
        else if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal) return false;
            if (bytes[0] is 0xfc or 0xfd) return false;
        }
        return true;
    }

    [HttpGet]
    public async Task<ActionResult<List<ApplicationResponse>>> GetAll()
    {
        var userId = GetUserId();

        var applications = await _context.JobApplications
            .Where(a => a.UserId == userId)
            .Select(a => new ApplicationResponse
            {
                Id = a.Id,
                CompanyName = a.CompanyName,
                JobTitle = a.JobTitle,
                JobDescription = a.JobDescription,
                Status = a.Status,
                Notes = a.Notes,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt
            })
            .ToListAsync();

        return Ok(applications);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApplicationResponse>> GetById(int id)
    {
        var userId = GetUserId();

        var application = await _context.JobApplications
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (application is null)
        {
            return NotFound();
        }

        return Ok(new ApplicationResponse
        {
            Id = application.Id,
            CompanyName = application.CompanyName,
            JobTitle = application.JobTitle,
            JobDescription = application.JobDescription,
            Status = application.Status,
            Notes = application.Notes,
            CreatedAt = application.CreatedAt,
            UpdatedAt = application.UpdatedAt
        });
    }

    [HttpPost]
    public async Task<ActionResult<ApplicationResponse>> Create(CreateApplicationRequest request)
    {
        var userId = GetUserId();

        var application = new JobApplication
        {
            UserId = userId,
            CompanyName = request.CompanyName,
            JobTitle = request.JobTitle,
            JobDescription = request.JobDescription,
            Notes = request.Notes
        };

        _context.JobApplications.Add(application);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = application.Id }, new ApplicationResponse
        {
            Id = application.Id,
            CompanyName = application.CompanyName,
            JobTitle = application.JobTitle,
            JobDescription = application.JobDescription,
            Status = application.Status,
            Notes = application.Notes,
            CreatedAt = application.CreatedAt,
            UpdatedAt = application.UpdatedAt
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateApplicationRequest request)
    {
        var userId = GetUserId();

        var application = await _context.JobApplications
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (application is null)
        {
            return NotFound();
        }

        application.CompanyName = request.CompanyName;
        application.JobTitle = request.JobTitle;
        application.JobDescription = request.JobDescription;
        application.Status = request.Status;
        application.Notes = request.Notes;
        application.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();

        var application = await _context.JobApplications
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (application is null)
        {
            return NotFound();
        }

        _context.JobApplications.Remove(application);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // A duplicate request already deleted this row — nothing left to do.
        }

        return NoContent();
    }
}
