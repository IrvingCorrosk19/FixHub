using FixHub.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using FixHub.Application.Common.Models;
using FixHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FixHub.Application.Features.Jobs;

// ─── Command ──────────────────────────────────────────────────────────────────
public record CancelJobCommand(Guid JobId, Guid CustomerId) : IRequest<Result<JobDto>>;

// ─── Handler ──────────────────────────────────────────────────────────────────
public class CancelJobCommandHandler(IApplicationDbContext db, IDashboardCacheInvalidator dashboardCache, ILogger<CancelJobCommandHandler> logger, INotificationService notifications)
    : IRequestHandler<CancelJobCommand, Result<JobDto>>
{
    public async Task<Result<JobDto>> Handle(CancelJobCommand req, CancellationToken ct)
    {
        var job = await db.Jobs
            .Include(j => j.Customer)
            .Include(j => j.Category)
            .Include(j => j.Assignment)
            .FirstOrDefaultAsync(j => j.Id == req.JobId, ct);

        if (job is null)
            return Result<JobDto>.Failure("Job not found.", "JOB_NOT_FOUND");

        if (job.CustomerId != req.CustomerId)
            return Result<JobDto>.Failure("Only the job owner can cancel it.", "FORBIDDEN");

        // Solo se puede cancelar en Open o Assigned
        if (job.Status == JobStatus.InProgress)
            return Result<JobDto>.Failure("Job cannot be cancelled: service is already in progress.", "INVALID_STATUS");
        if (job.Status == JobStatus.Completed)
            return Result<JobDto>.Failure("Job cannot be cancelled: service has already been completed.", "INVALID_STATUS");
        if (job.Status == JobStatus.Cancelled)
            return Result<JobDto>.Failure("Job is already cancelled.", "INVALID_STATUS");

        // Pre-cargar datos de notificación antes de abrir la transacción (solo lectura)
        Guid? assignedTechId = null;
        if (job.Assignment is not null)
        {
            var prop = await db.Proposals.FirstOrDefaultAsync(p => p.Id == job.Assignment.ProposalId, ct);
            assignedTechId = prop?.TechnicianId;
        }
        var adminIds = await db.Users.Where(u => u.Role == UserRole.Admin).Select(u => u.Id).ToListAsync(ct);

        // FASE 14: Transacción explícita + protección contra concurrencia (xmin)
        var statusBefore = job.Status.ToString();
        await using var transaction = await db.BeginTransactionAsync(ct);
        try
        {
            job.Status = JobStatus.Cancelled;
            job.CancelledAt = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(ct);
            return Result<JobDto>.Failure("Concurrent modification detected. Please retry.", "CONCURRENCY_CONFLICT");
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(ct);
            throw;
        }

        dashboardCache.Invalidate();
        logger.LogInformation("Job status changed. JobId={JobId} StatusBefore={StatusBefore} StatusAfter=Cancelled", job.Id, statusBefore);

        // Notificaciones FUERA de la transacción (Outbox pattern: tolerante a fallos)
        await notifications.NotifyAsync(job.CustomerId, FixHub.Domain.Enums.NotificationType.JobCancelled,
            "Tu solicitud ha sido cancelada.", job.Id, ct);

        if (assignedTechId.HasValue)
            await notifications.NotifyAsync(assignedTechId.Value, FixHub.Domain.Enums.NotificationType.JobCancelled,
                $"El cliente canceló la solicitud: {job.Title}", job.Id, ct);

        if (adminIds.Count > 0)
            await notifications.NotifyManyAsync(adminIds, FixHub.Domain.Enums.NotificationType.JobCancelled,
                $"Solicitud cancelada: {job.Title}", job.Id, ct);

        return Result<JobDto>.Success(
            job.ToDto(job.Customer.FullName, job.Category.Name));
    }
}
