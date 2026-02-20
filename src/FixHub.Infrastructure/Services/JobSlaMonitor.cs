using FixHub.Application.Common.Interfaces;
using FixHub.Domain.Entities;
using FixHub.Domain.Enums;
using FixHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FixHub.Infrastructure.Services;

/// <summary>
/// FASE 11: Motor SLA - evalúa reglas cada 2 minutos y genera alertas persistentes.
/// </summary>
public class JobSlaMonitor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<JobSlaMonitor> _log;
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(2);

    private static readonly TimeSpan OpenThreshold = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan AssignedNotStartedThreshold = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan InProgressThreshold = TimeSpan.FromHours(3);
    private static readonly TimeSpan IssueUnresolvedThreshold = TimeSpan.FromHours(1);

    public JobSlaMonitor(IServiceScopeFactory scopeFactory, ILogger<JobSlaMonitor> log)
    {
        _scopeFactory = scopeFactory;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.LogInformation("JobSlaMonitor started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EvaluateSlaRulesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error in JobSlaMonitor");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task EvaluateSlaRulesAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var now = DateTime.UtcNow;
        var openLimit = now - OpenThreshold;
        var assignedLimit = now - AssignedNotStartedThreshold;
        var inProgressLimit = now - InProgressThreshold;
        var issueLimit = now - IssueUnresolvedThreshold;

        var adminIds = await db.Users
            .Where(u => u.Role == UserRole.Admin)
            .Select(u => u.Id)
            .ToListAsync(ct);

        if (adminIds.Count == 0)
            return;

        var created = 0;

        // 1. Open > 15 min
        var openJobs = await db.Jobs
            .Where(j => j.Status == JobStatus.Open && j.CreatedAt < openLimit)
            .Select(j => new { j.Id, j.Title })
            .ToListAsync(ct);

        foreach (var j in openJobs)
        {
            if (await HasUnresolvedAlertAsync(db, j.Id, JobAlertType.OpenTooLong, ct))
                continue;
            await CreateAlertAndNotifyAsync(db, notificationService, j.Id, j.Title, JobAlertType.OpenTooLong,
                $"Trabajo '{j.Title}' lleva más de 15 minutos en estado Abierto.", adminIds, ct);
            created++;
        }

        // 2. Assigned > 30 min sin StartedAt
        var assignedJobs = await db.Jobs
            .Include(j => j.Assignment)
            .Where(j => j.Status == JobStatus.Assigned
                && j.AssignedAt != null
                && j.AssignedAt < assignedLimit
                && j.Assignment != null
                && j.Assignment.StartedAt == null)
            .Select(j => new { j.Id, j.Title })
            .ToListAsync(ct);

        foreach (var j in assignedJobs)
        {
            if (await HasUnresolvedAlertAsync(db, j.Id, JobAlertType.AssignedNotStarted, ct))
                continue;
            await CreateAlertAndNotifyAsync(db, notificationService, j.Id, j.Title, JobAlertType.AssignedNotStarted,
                $"Trabajo '{j.Title}' asignado hace más de 30 minutos sin iniciar.", adminIds, ct);
            created++;
        }

        // 3. InProgress > 3 horas (usa StartedAt del Assignment)
        var inProgressJobs = await db.Jobs
            .Include(j => j.Assignment)
            .Where(j => j.Status == JobStatus.InProgress
                && j.Assignment != null
                && j.Assignment.StartedAt != null
                && j.Assignment.StartedAt < inProgressLimit)
            .Select(j => new { j.Id, j.Title })
            .ToListAsync(ct);

        foreach (var j in inProgressJobs)
        {
            if (await HasUnresolvedAlertAsync(db, j.Id, JobAlertType.InProgressTooLong, ct))
                continue;
            await CreateAlertAndNotifyAsync(db, notificationService, j.Id, j.Title, JobAlertType.InProgressTooLong,
                $"Trabajo '{j.Title}' en progreso hace más de 3 horas.", adminIds, ct);
            created++;
        }

        // 4. Issue > 1 hora sin resolución (Job con JobIssue antiguo)
        var jobsWithOldIssues = await db.JobIssues
            .Where(i => i.CreatedAt < issueLimit)
            .Select(i => i.JobId)
            .Distinct()
            .ToListAsync(ct);

        foreach (var jobId in jobsWithOldIssues)
        {
            var job = await db.Jobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == jobId, ct);
            if (job == null) continue;

            if (job.Status == JobStatus.Completed || job.Status == JobStatus.Cancelled)
                continue;

            if (await HasUnresolvedAlertAsync(db, jobId, JobAlertType.IssueUnresolved, ct))
                continue;

            await CreateAlertAndNotifyAsync(db, notificationService, jobId, job.Title, JobAlertType.IssueUnresolved,
                $"Trabajo '{job.Title}' tiene incidencia(s) reportada(s) hace más de 1 hora sin resolver.", adminIds, ct);
            created++;
        }

        if (created > 0)
        {
            await db.SaveChangesAsync(ct);
            _log.LogInformation("JobSlaMonitor created {Count} alerts", created);
        }
    }

    private static async Task<bool> HasUnresolvedAlertAsync(AppDbContext db, Guid jobId, JobAlertType type, CancellationToken ct)
    {
        return await db.JobAlerts
            .AnyAsync(a => a.JobId == jobId && a.Type == type && !a.IsResolved, ct);
    }

    private static async Task CreateAlertAndNotifyAsync(
        AppDbContext db,
        INotificationService notificationService,
        Guid jobId,
        string jobTitle,
        JobAlertType alertType,
        string message,
        List<Guid> adminIds,
        CancellationToken ct)
    {
        var alert = new JobAlert
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            Type = alertType,
            Message = message,
            CreatedAt = DateTime.UtcNow,
            IsResolved = false
        };
        db.JobAlerts.Add(alert);
        await notificationService.NotifyManyAsync(adminIds, NotificationType.SlaAlert, message, jobId, ct);
    }
}
