using CareerPilot.Api.models;
using Microsoft.EntityFrameworkCore;

namespace CareerPilot.Api.data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<JobApplication> JobApplications { get; set; } = null!;
    public DbSet<Cv> Cvs { get; set; } = null!;
    public DbSet<CvAnalysis> CvAnalyses { get; set; } = null!;
}