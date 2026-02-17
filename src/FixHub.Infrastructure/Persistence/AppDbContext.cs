using FixHub.Application.Common.Interfaces;
using FixHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace FixHub.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<TechnicianProfile> TechnicianProfiles => Set<TechnicianProfile>();
    public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<Proposal> Proposals => Set<Proposal>();
    public DbSet<JobAssignment> JobAssignments => Set<JobAssignment>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<ScoreSnapshot> ScoreSnapshots => Set<ScoreSnapshot>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
