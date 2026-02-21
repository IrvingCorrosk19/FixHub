using FixHub.Application.Common.Interfaces;
using FixHub.Application.Common.Models;
using FixHub.Application.Features.Jobs;
using FixHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FixHub.Application.Features.Admin;

// ─── DTOs ─────────────────────────────────────────────────────────────────────

public record DashboardKpisDto(
    int TotalToday,
    int OpenToday,
    int AssignedToday,
    int InProgressToday,
    int CompletedToday,
    int CancelledToday,
    int IssuesLast24h,
    int? AvgAssignmentTimeMinutes,    // tiempo medio Open→Assigned (últimas 24h)
    int? AvgCompletionTimeMinutes,    // tiempo medio Assigned→Completed (últimas 24h)
    decimal? CancellationRateToday   // % cancelaciones sobre total del día
);

/// <summary>Job en alerta de SLA (demora o incidencia activa).</summary>
public record DashboardAlertJobDto(
    Guid JobId,
    string Title,
    string CustomerName,
    string Status,
    DateTime CreatedAt,
    int ElapsedMinutes,
    string AlertType,   // "open_overdue" | "inprogress_overdue" | "issue"
    string Severity     // "INFO" | "WARNING" | "CRITICAL"
);

/// <summary>Job reciente para la tabla de operaciones.</summary>
public record DashboardRecentJobDto(
    Guid JobId,
    string Title,
    string CustomerName,
    string CategoryName,
    string Status,
    DateTime CreatedAt
);

public record OpsDashboardDto(
    DashboardKpisDto Kpis,
    IReadOnlyList<DashboardAlertJobDto> Alerts,
    IReadOnlyList<DashboardRecentJobDto> RecentJobs,
    IReadOnlyList<IssueDto> RecentIssues
);

// ─── Query ────────────────────────────────────────────────────────────────────
public record GetOpsDashboardQuery : IRequest<Result<OpsDashboardDto>>;

// ─── Handler ──────────────────────────────────────────────────────────────────
public class GetOpsDashboardQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetOpsDashboardQuery, Result<OpsDashboardDto>>
{
    private const int OpenSlaSinceMinutes      = 15;
    private const int InProgressSlaSinceHours  = 2;
    private const int AlertsLimit              = 10;
    private const int RecentJobsLimit          = 20;
    private const int RecentIssuesLimit        = 10;

    public async Task<Result<OpsDashboardDto>> Handle(GetOpsDashboardQuery _, CancellationToken ct)
    {
        var utcNow   = DateTime.UtcNow;
        var todayUtc = utcNow.Date;

        // ── KPIs del día ──────────────────────────────────────────────────────
        var todayJobs = await db.Jobs
            .Where(j => j.CreatedAt >= todayUtc)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total       = g.Count(),
                Open        = g.Count(j => j.Status == JobStatus.Open),
                Assigned    = g.Count(j => j.Status == JobStatus.Assigned),
                InProgress  = g.Count(j => j.Status == JobStatus.InProgress),
                Completed   = g.Count(j => j.Status == JobStatus.Completed),
                Cancelled   = g.Count(j => j.Status == JobStatus.Cancelled)
            })
            .FirstOrDefaultAsync(ct);

        var issuesLast24h = await db.JobIssues
            .CountAsync(i => i.CreatedAt >= utcNow.AddHours(-24), ct);

        // ── Tiempos promedio (últimas 24h para muestra representativa) ────────
        // AvgAssignmentTime: Job.CreatedAt → JobAssignment.AcceptedAt
        // Traemos las fechas y calculamos la diferencia en memoria (compatible con Npgsql/PostgreSQL)
        var assignmentPairs = await db.Jobs
            .Where(j => j.Assignment != null && j.Assignment.AcceptedAt >= utcNow.AddHours(-24))
            .Select(j => new { j.CreatedAt, j.Assignment!.AcceptedAt })
            .ToListAsync(ct);

        int? avgAssignmentMinutes = assignmentPairs.Count > 0
            ? (int)assignmentPairs.Average(p => (p.AcceptedAt - p.CreatedAt).TotalMinutes)
            : null;

        // AvgCompletionTime: JobAssignment.AcceptedAt → Assignment.CompletedAt
        var completionPairs = await db.Jobs
            .Where(j => j.Assignment != null
                     && j.Assignment.CompletedAt != null
                     && j.Assignment.CompletedAt >= utcNow.AddHours(-24))
            .Select(j => new { j.Assignment!.AcceptedAt, CompletedAt = j.Assignment!.CompletedAt!.Value })
            .ToListAsync(ct);

        int? avgCompletionMinutes = completionPairs.Count > 0
            ? (int)completionPairs.Average(p => (p.CompletedAt - p.AcceptedAt).TotalMinutes)
            : null;

        // Tasa de cancelación del día
        var totalToday     = todayJobs?.Total     ?? 0;
        var cancelledToday = todayJobs?.Cancelled ?? 0;
        decimal? cancellationRateToday = totalToday > 0
            ? Math.Round((decimal)cancelledToday / totalToday * 100, 1)
            : null;

        var kpis = new DashboardKpisDto(
            totalToday,
            todayJobs?.Open       ?? 0,
            todayJobs?.Assigned   ?? 0,
            todayJobs?.InProgress ?? 0,
            todayJobs?.Completed  ?? 0,
            cancelledToday,
            issuesLast24h,
            avgAssignmentMinutes,
            avgCompletionMinutes,
            cancellationRateToday);

        // ── Alertas SLA ───────────────────────────────────────────────────────
        var openOverdueThreshold       = utcNow.AddMinutes(-OpenSlaSinceMinutes);
        var inProgressOverdueThreshold = utcNow.AddHours(-InProgressSlaSinceHours);

        // Open demasiado tiempo — severity según cuánto llevan esperando
        var openOverdueRaw = await db.Jobs
            .Include(j => j.Customer)
            .Where(j => j.Status == JobStatus.Open && j.CreatedAt <= openOverdueThreshold)
            .OrderBy(j => j.CreatedAt)
            .Take(AlertsLimit)
            .Select(j => new { j.Id, j.Title, j.Customer.FullName, j.Status, j.CreatedAt })
            .ToListAsync(ct);

        var openOverdue = openOverdueRaw.Select(j =>
        {
            var elapsed = (int)(utcNow - j.CreatedAt).TotalMinutes;
            // Open >45min=CRITICAL, >30min=WARNING, >15min=INFO
            var severity = elapsed >= 45 ? "CRITICAL" : elapsed >= 30 ? "WARNING" : "INFO";
            return new DashboardAlertJobDto(j.Id, j.Title, j.FullName, j.Status.ToString(), j.CreatedAt, elapsed, "open_overdue", severity);
        }).ToList();

        // InProgress demasiado tiempo — BUG-3 FIX: usar Assignment.StartedAt (no j.CreatedAt)
        // Evita falsos positivos: job creado hace 3h pero iniciado hace 5 min no debe alertar.
        var inProgressOverdueRaw = await db.Jobs
            .Where(j => j.Status == JobStatus.InProgress
                && j.Assignment != null
                && j.Assignment.StartedAt != null
                && j.Assignment.StartedAt <= inProgressOverdueThreshold)
            .OrderBy(j => j.Assignment!.StartedAt)
            .Take(AlertsLimit)
            .Select(j => new
            {
                j.Id,
                j.Title,
                CustomerName = j.Customer.FullName,
                j.Status,
                StartedAt = j.Assignment!.StartedAt!.Value
            })
            .ToListAsync(ct);

        var inProgressOverdue = inProgressOverdueRaw.Select(j =>
        {
            var elapsed = (int)(utcNow - j.StartedAt).TotalMinutes;
            // InProgress >3h=CRITICAL, >2h=WARNING
            var severity = elapsed >= 180 ? "CRITICAL" : "WARNING";
            return new DashboardAlertJobDto(j.Id, j.Title, j.CustomerName, j.Status.ToString(), j.StartedAt, elapsed, "inprogress_overdue", severity);
        }).ToList();

        // Jobs con incidencias últimas 24h (sin duplicar) — siempre CRITICAL
        var jobIdsWithIssues = await db.JobIssues
            .Where(i => i.CreatedAt >= utcNow.AddHours(-24))
            .Select(i => i.JobId)
            .Distinct()
            .ToListAsync(ct);

        var issueAlertsRaw = await db.Jobs
            .Include(j => j.Customer)
            .Where(j => jobIdsWithIssues.Contains(j.Id))
            .Select(j => new { j.Id, j.Title, j.Customer.FullName, j.Status, j.CreatedAt })
            .ToListAsync(ct);

        var issueAlerts = issueAlertsRaw.Select(j =>
        {
            var elapsed = (int)(utcNow - j.CreatedAt).TotalMinutes;
            return new DashboardAlertJobDto(j.Id, j.Title, j.FullName, j.Status.ToString(), j.CreatedAt, elapsed, "issue", "CRITICAL");
        }).ToList();

        // FASE 14: Excluir del dashboard jobs con alertas SLA ya resueltas.
        var resolvedJobIds = await db.JobAlerts
            .Where(a => a.IsResolved)
            .Select(a => a.JobId)
            .Distinct()
            .ToListAsync(ct);

        // Ordenar: CRITICAL primero, luego WARNING, luego INFO; dentro de cada grupo por elapsed desc
        static int SeverityOrder(string s) => s switch { "CRITICAL" => 0, "WARNING" => 1, _ => 2 };

        var alerts = openOverdue
            .Concat(inProgressOverdue)
            .Concat(issueAlerts)
            .Where(a => !resolvedJobIds.Contains(a.JobId))   // Solo alertas activas
            .DistinctBy(a => a.JobId)
            .OrderBy(a => SeverityOrder(a.Severity))
            .ThenByDescending(a => a.ElapsedMinutes)
            .Take(AlertsLimit)
            .ToList();

        // ── Trabajos recientes ────────────────────────────────────────────────
        var recentJobs = await db.Jobs
            .Include(j => j.Customer)
            .Include(j => j.Category)
            .OrderByDescending(j => j.CreatedAt)
            .Take(RecentJobsLimit)
            .Select(j => new DashboardRecentJobDto(
                j.Id,
                j.Title,
                j.Customer.FullName,
                j.Category.Name,
                j.Status.ToString(),
                j.CreatedAt))
            .ToListAsync(ct);

        // ── Incidencias recientes ─────────────────────────────────────────────
        var recentIssues = await db.JobIssues
            .Include(i => i.Job)
            .Include(i => i.ReportedBy)
            .OrderByDescending(i => i.CreatedAt)
            .Take(RecentIssuesLimit)
            .Select(i => new IssueDto(
                i.Id,
                i.JobId,
                i.Job.Title,
                i.ReportedBy.FullName,
                i.Reason,
                i.Detail,
                i.CreatedAt,
                i.ResolvedAt,
                i.ResolvedByUserId,
                i.ResolutionNote))
            .ToListAsync(ct);

        return Result<OpsDashboardDto>.Success(new OpsDashboardDto(
            kpis, alerts, recentJobs, recentIssues));
    }
}
