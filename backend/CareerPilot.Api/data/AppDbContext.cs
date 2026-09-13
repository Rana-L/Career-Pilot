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
    public DbSet<SavedJobSearch> SavedJobSearches { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SavedJobSearch>()
            .HasIndex(s => s.UserId)
            .IsUnique();
    }
}