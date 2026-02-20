using FixHub.Application.Common.Interfaces;
using FixHub.Application.Common.Models;
using FixHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FixHub.Application.Features.Admin;

/// <summary>
/// FASE 14: Métricas operativas para hardening y monitoreo.
/// </summary>
public record AdminMetricsDto(
    int TotalEmailsSentToday,
    int TotalEmailsFailedToday,
    int TotalSlaAlertsToday,
    double? AvgMinutesOpenToAssigned,
    double? AvgMinutesAssignedToCompleted
);

public record GetAdminMetricsQuery : IRequest<Result<AdminMetricsDto>>;

public class GetAdminMetricsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAdminMetricsQuery, Result<AdminMetricsDto>>
{
    public async Task<Result<AdminMetricsDto>> Handle(GetAdminMetricsQuery _, CancellationToken ct)
    {
        var todayUtc = DateTime.UtcNow.Date;

        var totalEmailsSentToday = await db.NotificationOutbox
            .CountAsync(o => o.Status == OutboxStatus.Sent && o.SentAt != null && o.SentAt.Value >= todayUtc, ct);

        var totalEmailsFailedToday = await db.NotificationOutbox
            .CountAsync(o => o.Status == OutboxStatus.Failed && o.CreatedAt >= todayUtc, ct);

        var totalSlaAlertsToday = await db.JobAlerts
            .CountAsync(a => a.CreatedAt >= todayUtc, ct);

        var assignmentPairs = await db.Jobs
            .Where(j => j.AssignedAt != null && j.AssignedAt >= todayUtc)
            .Select(j => new { j.CreatedAt, AssignedAt = j.AssignedAt!.Value })
            .ToListAsync(ct);

        double? avgOpenToAssigned = assignmentPairs.Count > 0
            ? assignmentPairs.Average(p => (p.AssignedAt - p.CreatedAt).TotalMinutes)
            : null;

        var completionPairs = await db.Jobs
            .Include(j => j.Assignment)
            .Where(j => j.Assignment != null
                && j.Assignment.CompletedAt != null
                && j.Assignment.CompletedAt >= todayUtc
                && j.AssignedAt != null)
            .Select(j => new { j.AssignedAt, CompletedAt = j.Assignment!.CompletedAt!.Value })
            .ToListAsync(ct);

        double? avgAssignedToCompleted = completionPairs.Count > 0
            ? completionPairs.Average(p => (p.CompletedAt - p.AssignedAt!.Value).TotalMinutes)
            : null;

        var dto = new AdminMetricsDto(
            totalEmailsSentToday,
            totalEmailsFailedToday,
            totalSlaAlertsToday,
            avgOpenToAssigned != null ? Math.Round(avgOpenToAssigned.Value, 1) : null,
            avgAssignedToCompleted != null ? Math.Round(avgAssignedToCompleted.Value, 1) : null);

        return Result<AdminMetricsDto>.Success(dto);
    }
}
