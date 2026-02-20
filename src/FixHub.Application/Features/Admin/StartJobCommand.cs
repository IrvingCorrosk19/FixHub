using FixHub.Application.Common.Interfaces;
using FixHub.Application.Common.Models;
using FixHub.Application.Features.Jobs;
using FixHub.Domain.Enums;
using Microsoft.Extensions.Logging;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FixHub.Application.Features.Admin;

/// <summary>
/// Fuerza la transición Open/Assigned → InProgress desde Admin.
/// Marca también StartedAt en la asignación si existe.
/// </summary>
public record StartJobCommand(Guid JobId, Guid AdminUserId) : IRequest<Result<JobDto>>;

public class StartJobCommandHandler(IApplicationDbContext db, ILogger<StartJobCommandHandler> logger, INotificationService notifications)
    : IRequestHandler<StartJobCommand, Result<JobDto>>
{
    public async Task<Result<JobDto>> Handle(StartJobCommand req, CancellationToken ct)
    {
        var job = await db.Jobs
            .Include(j => j.Customer)
            .Include(j => j.Category)
            .Include(j => j.Assignment)
            .FirstOrDefaultAsync(j => j.Id == req.JobId, ct);

        if (job is null)
            return Result<JobDto>.Failure("Solicitud no encontrada.", "JOB_NOT_FOUND");

        if (job.Status != JobStatus.Open && job.Status != JobStatus.Assigned)
            return Result<JobDto>.Failure(
                $"Solo se puede iniciar un trabajo en estado Open o Assigned. Estado actual: {job.Status}",
                "INVALID_STATUS");

        var statusBefore = job.Status.ToString();
        job.Status = JobStatus.InProgress;

        if (job.Assignment is not null)
            job.Assignment.StartedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Job status changed. JobId={JobId} StatusBefore={StatusBefore} StatusAfter=InProgress",
            job.Id, statusBefore);

        await notifications.NotifyAsync(job.CustomerId, NotificationType.JobStarted,
            "El técnico está en camino.", job.Id, ct);
        if (job.Assignment is not null)
        {
            var techId = (await db.Proposals.FirstOrDefaultAsync(p => p.Id == job.Assignment.ProposalId, ct))?.TechnicianId;
            if (techId.HasValue)
                await notifications.NotifyAsync(techId.Value, NotificationType.JobStarted, "Puedes iniciar el servicio.", job.Id, ct);
        }

        return Result<JobDto>.Success(
            job.ToDto(job.Customer.FullName, job.Category.Name));
    }
}
