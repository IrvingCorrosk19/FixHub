using FixHub.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using FixHub.Application.Common.Models;
using FixHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FixHub.Application.Features.Jobs;

// ─── Command ──────────────────────────────────────────────────────────────────
public record CompleteJobCommand(Guid JobId, Guid CustomerId) : IRequest<Result<JobDto>>;

// ─── Handler ──────────────────────────────────────────────────────────────────
public class CompleteJobCommandHandler(IApplicationDbContext db, ILogger<CompleteJobCommandHandler> logger, INotificationService notifications)
    : IRequestHandler<CompleteJobCommand, Result<JobDto>>
{
    public async Task<Result<JobDto>> Handle(CompleteJobCommand req, CancellationToken ct)
    {
        var job = await db.Jobs
            .Include(j => j.Customer)
            .Include(j => j.Category)
            .Include(j => j.Assignment)
            .FirstOrDefaultAsync(j => j.Id == req.JobId, ct);

        if (job is null)
            return Result<JobDto>.Failure("Job not found.", "JOB_NOT_FOUND");

        if (job.CustomerId != req.CustomerId)
            return Result<JobDto>.Failure("Only the job owner can complete it.", "FORBIDDEN");

        if (job.Status != JobStatus.InProgress && job.Status != JobStatus.Assigned)
            return Result<JobDto>.Failure(
                $"Job must be InProgress or Assigned to complete. Current status: {job.Status}",
                "INVALID_STATUS");

        var statusBefore = job.Status.ToString();
        job.Status = JobStatus.Completed;
        job.CompletedAt = DateTime.UtcNow;

        if (job.Assignment is not null)
            job.Assignment.CompletedAt = DateTime.UtcNow;

        // Actualizar métricas del técnico
        if (job.Assignment is not null)
        {
            var proposal = await db.Proposals
                .FirstOrDefaultAsync(p => p.Id == job.Assignment.ProposalId, ct);

            if (proposal is not null)
            {
                var techProfile = await db.TechnicianProfiles
                    .FirstOrDefaultAsync(tp => tp.UserId == proposal.TechnicianId, ct);

                if (techProfile is not null)
                    techProfile.CompletedJobs++;
            }
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Job status changed. JobId={JobId} StatusBefore={StatusBefore} StatusAfter=Completed",
            job.Id, statusBefore);

        // Notificar al Cliente (FASE 13)
        await notifications.NotifyAsync(job.CustomerId, FixHub.Domain.Enums.NotificationType.JobCompleted,
            "Tu servicio ha sido completado. ¡Gracias por confiar en FixHub! Califica al técnico cuando puedas.", job.Id, ct);

        if (job.Assignment is not null)
        {
            var prop = await db.Proposals.FirstOrDefaultAsync(p => p.Id == job.Assignment.ProposalId, ct);
            if (prop is not null)
                await notifications.NotifyAsync(prop.TechnicianId, FixHub.Domain.Enums.NotificationType.JobCompleted,
                    "El cliente ha confirmado la finalización del servicio.", job.Id, ct);
        }

        return Result<JobDto>.Success(
            job.ToDto(job.Customer.FullName, job.Category.Name));
    }
}
