using FixHub.Application.Common.Interfaces;
using FixHub.Application.Common.Models;
using FixHub.Application.Features.Jobs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FixHub.Application.Features.Admin;

// ─── Query ────────────────────────────────────────────────────────────────────
public record ListJobIssuesQuery(int Page, int PageSize) : IRequest<Result<PagedResult<IssueDto>>>;

// ─── Handler ──────────────────────────────────────────────────────────────────
public class ListJobIssuesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<ListJobIssuesQuery, Result<PagedResult<IssueDto>>>
{
    public async Task<Result<PagedResult<IssueDto>>> Handle(ListJobIssuesQuery req, CancellationToken ct)
    {
        var query = db.JobIssues
            .Include(i => i.Job)
            .Include(i => i.ReportedBy)
            .OrderByDescending(i => i.CreatedAt);

        var total = await query.CountAsync(ct);

        var items = await query
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(i => new IssueDto(
                i.Id,
                i.JobId,
                i.Job.Title,
                i.ReportedBy.FullName,
                i.Reason,
                i.Detail,
                i.CreatedAt))
            .ToListAsync(ct);

        return Result<PagedResult<IssueDto>>.Success(new PagedResult<IssueDto>
        {
            Items = items,
            TotalCount = total,
            Page = req.Page,
            PageSize = req.PageSize
        });
    }
}
