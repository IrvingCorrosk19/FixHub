using FixHub.Application.Common.Interfaces;
using FixHub.Application.Common.Models;
using FixHub.Application.Features.Jobs;
using FixHub.Domain.Enums;
using Microsoft.Extensions.Logging;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FixHub.Application.Features.Admin;

/// <summary>
/// Permite a Admin forzar un cambio de estado en cualquier job
/// (InProgress, Completed, Cancelled) sin validar propietario.
/// </summary>
public record AdminUpdateJobStatusCommand(Guid JobId, string NewStatus, Guid AdminUserId) : IRequest<Result<JobDto>>;

public class AdminUpdateJobStatusCommandHandler(IApplicationDbContext db, IDashboardCacheInvalidator dashboardCache, ILogger<AdminUpdateJobStatusCommandHandler> logger, INotificationService notifications)
    : IRequestHandler<AdminUpdateJobStatusCommand, Result<JobDto>>
{
    // Transiciones permitidas desde Admin (no todas las combinaciones son válidas)
    private static readonly Dictionary<JobStatus, JobStatus[]> AllowedTransitions = new()
    {
        [JobStatus.Open]        = [JobStatus.InProgress, JobStatus.Cancelled],
        [JobStatus.Assigned]    = [JobStatus.InProgress, JobStatus.Cancelled],
        [JobStatus.InProgress]  = [JobStatus.Completed, JobStatus.Cancelled],
    };

    public async Task<Result<JobDto>> Handle(AdminUpdateJobStatusCommand req, CancellationToken ct)
    {
        if (!Enum.TryParse<JobStatus>(req.NewStatus, ignoreCase: true, out var newStatus))
            return Result<JobDto>.Failure($"Estado inválido: {req.NewStatus}", "INVALID_STATUS");

        var job = await db.Jobs
            .Include(j => j.Customer)
            .Include(j => j.Category)
            .Include(j => j.Assignment)
            .FirstOrDefaultAsync(j => j.Id == req.JobId, ct);

        if (job is null)
            return Result<JobDto>.Failure("Solicitud no encontrada.", "JOB_NOT_FOUND");

        if (!AllowedTransitions.TryGetValue(job.Status, out var allowed) || !allowed.Contains(newStatus))
            return Result<JobDto>.Failure(
                $"No se puede pasar de {job.Status} a {newStatus}.", "INVALID_TRANSITION");

        var statusBefore = job.Status.ToString();
        var now = DateTime.UtcNow;
        job.Status = newStatus;

        if (newStatus == JobStatus.InProgress && job.Assignment is not null && job.Assignment.StartedAt is null)
            job.Assignment.StartedAt = now;
        if (newStatus == JobStatus.Completed)
        {
            job.CompletedAt = now;
            if (job.Assignment is not null)
                job.Assignment.CompletedAt = now;
        }
        if (newStatus == JobStatus.Cancelled)
            job.CancelledAt = now;

        await db.SaveChangesAsync(ct);
        dashboardCache.Invalidate();
        logger.LogInformation("Job status changed. JobId={JobId} StatusBefore={StatusBefore} StatusAfter={StatusAfter}",
            job.Id, statusBefore, newStatus.ToString());

        if (newStatus == JobStatus.InProgress)
        {
            await notifications.NotifyAsync(job.CustomerId, NotificationType.JobStarted, "El técnico está en camino.", job.Id, ct);
            if (job.Assignment is not null)
            {
                var prop = await db.Proposals.FirstOrDefaultAsync(p => p.Id == job.Assignment.ProposalId, ct);
                if (prop is not null)
                    await notifications.NotifyAsync(prop.TechnicianId, NotificationType.JobStarted, "Puedes iniciar el servicio.", job.Id, ct);
            }
        }
        else if (newStatus == JobStatus.Completed)
        {
            await notifications.NotifyAsync(job.CustomerId, NotificationType.JobCompleted, "Servicio completado.", job.Id, ct);
            if (job.Assignment is not null)
            {
                var prop = await db.Proposals.FirstOrDefaultAsync(p => p.Id == job.Assignment.ProposalId, ct);
                if (prop is not null)
                    await notifications.NotifyAsync(prop.TechnicianId, NotificationType.JobCompleted, "El trabajo ha sido marcado como completado.", job.Id, ct);
            }
        }
        else if (newStatus == JobStatus.Cancelled)
        {
            await notifications.NotifyAsync(job.CustomerId, NotificationType.JobCancelled, "La solicitud ha sido cancelada.", job.Id, ct);
        }

        return Result<JobDto>.Success(
            job.ToDto(job.Customer.FullName, job.Category.Name));
    }
}
