using FixHub.Application.Common.Interfaces;
using FixHub.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FixHub.Application.Features.Jobs;

// ─── Query: Single Job ────────────────────────────────────────────────────────
public record GetJobQuery(Guid JobId) : IRequest<Result<JobDto>>;

public class GetJobQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetJobQuery, Result<JobDto>>
{
    public async Task<Result<JobDto>> Handle(GetJobQuery req, CancellationToken ct)
    {
        var job = await db.Jobs
            .Include(j => j.Customer)
            .Include(j => j.Category)
            .FirstOrDefaultAsync(j => j.Id == req.JobId, ct);

        if (job is null)
            return Result<JobDto>.Failure("Job not found.", "JOB_NOT_FOUND");

        return Result<JobDto>.Success(
            job.ToDto(job.Customer.FullName, job.Category.Name));
    }
}
