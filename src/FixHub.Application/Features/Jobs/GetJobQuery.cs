using FixHub.Application.Common.Interfaces;
using FixHub.Application.Common.Models;
using FixHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FixHub.Application.Features.Jobs;

// ─── Query: Single Job (FASE 8: autorización por rol — empresa de servicios) ───
public record GetJobQuery(
    Guid JobId,
    Guid RequesterId,
    string RequesterRole
) : IRequest<Result<JobDto>>;

public class GetJobQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetJobQuery, Result<JobDto>>
{
    public async Task<Result<JobDto>> Handle(GetJobQuery req, CancellationToken ct)
    {
        var job = await db.Jobs
            .Include(j => j.Customer)
            .Include(j => j.Category)
            .Include(j => j.Assignment)
                .ThenInclude(a => a!.Proposal)
                .ThenInclude(p => p.Technician)
            .Include(j => j.Proposals)
            .FirstOrDefaultAsync(j => j.Id == req.JobId, ct);

        if (job is null)
            return Result<JobDto>.Failure("Job not found.", "JOB_NOT_FOUND");

        // Admin: puede ver todo
        if (req.RequesterRole == "Admin")
        {
            var assignedId = job.Assignment?.Proposal?.TechnicianId;
            var assignedName = job.Assignment?.Proposal?.Technician?.FullName;
            return Result<JobDto>.Success(
                job.ToDto(job.Customer.FullName, job.Category.Name, assignedId, assignedName));
        }

        // Customer: solo sus propios jobs
        if (req.RequesterRole == "Customer")
        {
            if (job.CustomerId != req.RequesterId)
                return Result<JobDto>.Failure("Access denied to this job.", "FORBIDDEN");
            var assignedId = job.Assignment?.Proposal?.TechnicianId;
            var assignedName = job.Assignment?.Proposal?.Technician?.FullName;
            return Result<JobDto>.Success(
                job.ToDto(job.Customer.FullName, job.Category.Name, assignedId, assignedName));
        }

        // Technician: jobs asignados, Open (oportunidades) o donde tiene propuesta
        if (req.RequesterRole == "Technician")
        {
            var isAssigned = job.Assignment?.Proposal?.TechnicianId == req.RequesterId;
            var isOpen = job.Status == JobStatus.Open;
            var hasOwnProposal = job.Proposals.Any(p => p.TechnicianId == req.RequesterId);
            if (!isAssigned && !isOpen && !hasOwnProposal)
                return Result<JobDto>.Failure("Access denied to this job.", "FORBIDDEN");
            var assignedId = job.Assignment?.Proposal?.TechnicianId;
            var assignedName = job.Assignment?.Proposal?.Technician?.FullName;
            return Result<JobDto>.Success(
                job.ToDto(job.Customer.FullName, job.Category.Name, assignedId, assignedName));
        }

        return Result<JobDto>.Failure("Access denied to this job.", "FORBIDDEN");
    }
}
