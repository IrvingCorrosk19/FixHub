using FixHub.Application.Common.Interfaces;
using FixHub.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FixHub.Application.Features.Jobs;

// ─── Query ────────────────────────────────────────────────────────────────────
/// <summary>Returns jobs created by the current customer, newest first.</summary>
public record ListMyJobsQuery(Guid CustomerId, int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedResult<JobDto>>>;

// ─── Handler ──────────────────────────────────────────────────────────────────
public class ListMyJobsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<ListMyJobsQuery, Result<PagedResult<JobDto>>>
{
    public async Task<Result<PagedResult<JobDto>>> Handle(ListMyJobsQuery req, CancellationToken ct)
    {
        var query = db.Jobs
            .Include(j => j.Customer)
            .Include(j => j.Category)
            .Where(j => j.CustomerId == req.CustomerId)
            .OrderByDescending(j => j.CreatedAt);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .ToListAsync(ct);

        var dtos = items.Select(j => j.ToDto(j.Customer.FullName, j.Category.Name)).ToList();

        return Result<PagedResult<JobDto>>.Success(new PagedResult<JobDto>
        {
            Items = dtos,
            TotalCount = total,
            Page = req.Page,
            PageSize = req.PageSize
        });
    }
}
