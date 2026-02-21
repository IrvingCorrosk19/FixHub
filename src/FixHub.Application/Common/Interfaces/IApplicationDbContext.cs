using FixHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FixHub.Application.Common.Interfaces;

/// <summary>
/// Abstracción del DbContext expuesta a Application.
/// Application NO referencia EF Core directamente; solo usa esta interfaz.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<TechnicianProfile> TechnicianProfiles { get; }
    DbSet<ServiceCategory> ServiceCategories { get; }
    DbSet<Job> Jobs { get; }
    DbSet<Proposal> Proposals { get; }
    DbSet<JobAssignment> JobAssignments { get; }
    DbSet<Review> Reviews { get; }
    DbSet<Payment> Payments { get; }
    DbSet<ScoreSnapshot> ScoreSnapshots { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<JobIssue> JobIssues { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<NotificationOutbox> NotificationOutbox { get; }
    DbSet<JobAlert> JobAlerts { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>FASE 14: Transacciones explícitas para operaciones críticas.</summary>
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
