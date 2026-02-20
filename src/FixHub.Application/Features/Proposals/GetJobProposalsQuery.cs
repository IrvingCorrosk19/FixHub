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

        var query = db.Proposals
            .Include(p => p.Technician)
            .Where(p => p.JobId == req.JobId);

        // Admin: ve todas las propuestas. Technician: solo las suyas (para saber si ya postuló). Customer: no ve.
        if (req.IsAdmin)
        {
            var proposals = await query.OrderByDescending(p => p.CreatedAt).ToListAsync(ct);
            return Result<List<ProposalDto>>.Success(
                proposals.Select(p => p.ToDto(p.Technician.FullName)).ToList());
        }

        var myProposals = await query
            .Where(p => p.TechnicianId == req.RequesterId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);
        return Result<List<ProposalDto>>.Success(
            myProposals.Select(p => p.ToDto(p.Technician.FullName)).ToList());
    }
}
