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

namespace CareerPilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApplicationsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ChatClient _chatClient;

    public ApplicationsController(AppDbContext context, ChatClient chatClient)
    {
        _context = context;
        _chatClient = chatClient;
    }

    private int GetUserId()
    {
        return int.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
    }

    [HttpPost("parse")]
    public async Task<ActionResult<ParseJobResponse>> Parse(ParseJobRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest("Paste a job posting first.");

        var prompt = $@"Extract the company name, job title, and job description from the job posting below.
Respond ONLY with a JSON object in this exact shape:
{{""companyName"": ""..."", ""jobTitle"": ""..."", ""jobDescription"": ""...""}}
For jobDescription, include the responsibilities and requirements as clean plain text; leave out
boilerplate like equal-opportunity statements, benefits fluff, and application instructions. If a field
can't be found, use an empty string.

Job posting:
{request.Text}";

        var chatResponse = await _chatClient.CompleteChatAsync(
            [new UserChatMessage(prompt)],
            new ChatCompletionOptions { ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat() });

        var json = chatResponse.Value.Content[0].Text;
        var parsed = JsonSerializer.Deserialize<ParseJobResponse>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        return Ok(parsed);
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
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
