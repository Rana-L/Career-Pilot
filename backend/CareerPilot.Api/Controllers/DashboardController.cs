using CareerPilot.Api.data;
using CareerPilot.Api.dto;
using CareerPilot.Api.models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CareerPilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    private int GetUserId()
    {
        return int.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
    }

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryResponse>> GetSummary()
    {
        var userId = GetUserId();

        var counts = await _context.JobApplications
            .Where(a => a.UserId == userId)
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var summary = new DashboardSummaryResponse();

        foreach (var group in counts)
        {
            switch (group.Status)
            {
                case ApplicationStatus.Wishlist:
                    summary.Wishlist = group.Count;
                    break;
                case ApplicationStatus.Applied:
                    summary.Applied = group.Count;
                    break;
                case ApplicationStatus.Assessment:
                    summary.Assessment = group.Count;
                    break;
                case ApplicationStatus.Interview:
                    summary.Interview = group.Count;
                    break;
                case ApplicationStatus.Offer:
                    summary.Offer = group.Count;
                    break;
                case ApplicationStatus.Rejected:
                    summary.Rejected = group.Count;
                    break;
            }

            summary.Total += group.Count;
        }

        return Ok(summary);
    }
}
