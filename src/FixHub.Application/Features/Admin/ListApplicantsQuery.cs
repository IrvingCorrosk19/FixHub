using FixHub.Application.Common.Interfaces;
using FixHub.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FixHub.Application.Features.Admin;

public record ApplicantDto(
    Guid UserId,
    string FullName,
    string Email,
    string? Phone,
    string Status,
    DateTime CreatedAt);

public record ListApplicantsQuery(int Page = 1, int PageSize = 20, string? Status = null)
    : IRequest<Result<PagedResult<ApplicantDto>>>;

public class ListApplicantsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<ListApplicantsQuery, Result<PagedResult<ApplicantDto>>>
{
    public async Task<Result<PagedResult<ApplicantDto>>> Handle(
        ListApplicantsQuery req, CancellationToken ct)
    {
        var query = db.TechnicianProfiles
            .Include(tp => tp.User)
            .AsQueryable();

        if (!string.IsNullOrEmpty(req.Status) &&
            int.TryParse(req.Status, out var statusInt))
            query = query.Where(tp => (int)tp.Status == statusInt);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(tp => tp.User.CreatedAt)
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(tp => new ApplicantDto(
                tp.UserId,
                tp.User.FullName,
                tp.User.Email,
                tp.User.Phone,
                tp.Status.ToString(),
                tp.User.CreatedAt))
            .ToListAsync(ct);

        return Result<PagedResult<ApplicantDto>>.Success(new PagedResult<ApplicantDto>
        {
            Items = items,
            TotalCount = total,
            Page = req.Page,
            PageSize = req.PageSize
        });
    }
}
