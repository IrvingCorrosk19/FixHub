using FixHub.Application.Common.Interfaces;
using FixHub.Application.Common.Models;
using FixHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FixHub.Application.Features.Jobs;

/// <summary>
/// Técnico asignado marca su trabajo como InProgress (inicia el servicio en sitio).
/// Solo el técnico de la asignación activa puede invocar este comando.
/// </summary>
public record TechnicianStartJobCommand(Guid JobId, Guid TechnicianId) : IRequest<Result<JobDto>>;

public class TechnicianStartJobCommandHandler(
    IApplicationDbContext db,
    IDashboardCacheInvalidator dashboardCache,
    ILogger<TechnicianStartJobCommandHandler> logger,
    INotificationService notifications)
    : IRequestHandler<TechnicianStartJobCommand, Result<JobDto>>
{
    public async Task<Result<JobDto>> Handle(TechnicianStartJobCommand req, CancellationToken ct)
    {
        var job = await db.Jobs
            .Include(j => j.Customer)
            .Include(j => j.Category)
            .Include(j => j.Assignment)
                .ThenInclude(a => a!.Proposal)
                    .ThenInclude(p => p.Technician)
            .FirstOrDefaultAsync(j => j.Id == req.JobId, ct);

        if (job is null)
            return Result<JobDto>.Failure("Job not found.", "JOB_NOT_FOUND");

        // Solo el técnico de la asignación puede iniciar el trabajo
        if (job.Assignment?.Proposal?.TechnicianId != req.TechnicianId)
            return Result<JobDto>.Failure("Only the assigned technician can start this job.", "FORBIDDEN");

        if (job.Status != JobStatus.Assigned)
            return Result<JobDto>.Failure(
                $"Job must be in Assigned status to start. Current status: {job.Status}",
                "INVALID_STATUS");

        await using var transaction = await db.BeginTransactionAsync(ct);
        try
        {
            job.Status = JobStatus.InProgress;
            job.Assignment!.StartedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            dashboardCache.Invalidate();
            logger.LogInformation(
                "Job started by technician. JobId={JobId} TechnicianId={TechnicianId}",
                job.Id, req.TechnicianId);

            // Notificar al cliente fuera de la transacción (Outbox pattern)
            await notifications.NotifyAsync(job.CustomerId, NotificationType.JobStarted,
                "El técnico ha iniciado el servicio.", job.Id, ct);

            var techId   = job.Assignment.Proposal?.TechnicianId;
            var techName = job.Assignment.Proposal?.Technician?.FullName;

            return Result<JobDto>.Success(
                job.ToDto(job.Customer.FullName, job.Category.Name, techId, techName));
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(ct);
            return Result<JobDto>.Failure(
                "Concurrent modification detected. Please retry.", "CONCURRENCY_CONFLICT");
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}
