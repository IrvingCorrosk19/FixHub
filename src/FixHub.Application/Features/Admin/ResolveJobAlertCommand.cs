using FixHub.Application.Common.Interfaces;
using FixHub.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FixHub.Application.Features.Admin;

// ─── Command ──────────────────────────────────────────────────────────────────
/// <summary>FASE 14: Resuelve una alerta SLA. Solo Admin. No permite doble resolución.</summary>
public record ResolveJobAlertCommand(
    Guid AlertId,
    Guid ResolvedByUserId
) : IRequest<Result<object>>;

// ─── Handler ──────────────────────────────────────────────────────────────────
public class ResolveJobAlertCommandHandler(IApplicationDbContext db, IDashboardCacheInvalidator dashboardCache)
    : IRequestHandler<ResolveJobAlertCommand, Result<object>>
{
    public async Task<Result<object>> Handle(ResolveJobAlertCommand req, CancellationToken ct)
    {
        var alert = await db.JobAlerts
            .FirstOrDefaultAsync(a => a.Id == req.AlertId, ct);

        if (alert is null)
            return Result<object>.Failure("Alert not found.", "NOT_FOUND");

        if (alert.IsResolved)
            return Result<object>.Failure("This alert has already been resolved.", "ALREADY_RESOLVED");

        alert.IsResolved = true;
        alert.ResolvedAt = DateTime.UtcNow;
        alert.ResolvedByUserId = req.ResolvedByUserId;

        await db.SaveChangesAsync(ct);
        dashboardCache.Invalidate();

        return Result<object>.Success(null!);
    }
}
