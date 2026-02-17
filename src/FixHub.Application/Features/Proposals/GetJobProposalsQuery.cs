using FixHub.Application.Common.Interfaces;
using FixHub.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FixHub.Application.Features.Proposals;

// ─── Query ────────────────────────────────────────────────────────────────────
public record GetJobProposalsQuery(Guid JobId, Guid RequesterId, bool IsAdmin)
    : IRequest<Result<List<ProposalDto>>>;

// ─── Handler ──────────────────────────────────────────────────────────────────
public class GetJobProposalsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetJobProposalsQuery, Result<List<ProposalDto>>>
{
    public async Task<Result<List<ProposalDto>>> Handle(
        GetJobProposalsQuery req, CancellationToken ct)
    {
        var job = await db.Jobs.FirstOrDefaultAsync(j => j.Id == req.JobId, ct);

        if (job is null)
            return Result<List<ProposalDto>>.Failure("Job not found.", "JOB_NOT_FOUND");

        // Solo el Customer dueño del job o Admin puede ver las proposals
        if (!req.IsAdmin && job.CustomerId != req.RequesterId)
            return Result<List<ProposalDto>>.Failure(
                "Access denied.", "FORBIDDEN");

        var proposals = await db.Proposals
            .Include(p => p.Technician)
            .Where(p => p.JobId == req.JobId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

        return Result<List<ProposalDto>>.Success(
            proposals.Select(p => p.ToDto(p.Technician.FullName)).ToList());
    }
}
