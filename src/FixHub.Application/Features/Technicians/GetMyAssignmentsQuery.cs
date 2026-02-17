using FixHub.Application.Common.Interfaces;
using FixHub.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FixHub.Application.Features.Technicians;

// ─── DTO ──────────────────────────────────────────────────────────────────────
public record AssignmentDto(
    Guid AssignmentId,
    Guid JobId,
    string JobTitle,
    string CategoryName,
    string CustomerName,
    string AddressText,
    decimal AcceptedPrice,
    string JobStatus,
    DateTime AcceptedAt,
    DateTime? CompletedAt);

// ─── Query ────────────────────────────────────────────────────────────────────
/// <summary>Returns jobs assigned to the current technician.</summary>
public record GetMyAssignmentsQuery(Guid TechnicianId, int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedResult<AssignmentDto>>>;

// ─── Handler ──────────────────────────────────────────────────────────────────
public class GetMyAssignmentsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetMyAssignmentsQuery, Result<PagedResult<AssignmentDto>>>
{
    public async Task<Result<PagedResult<AssignmentDto>>> Handle(
        GetMyAssignmentsQuery req, CancellationToken ct)
    {
        // Proposals of this technician that were accepted
        var query = db.JobAssignments
            .Include(a => a.Proposal)
            .Include(a => a.Job).ThenInclude(j => j.Customer)
            .Include(a => a.Job).ThenInclude(j => j.Category)
            .Where(a => a.Proposal.TechnicianId == req.TechnicianId)
            .OrderByDescending(a => a.AcceptedAt);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .ToListAsync(ct);

        var dtos = items.Select(a => new AssignmentDto(
            a.Id,
            a.JobId,
            a.Job.Title,
            a.Job.Category.Name,
            a.Job.Customer.FullName,
            a.Job.AddressText,
            a.Proposal.Price,
            a.Job.Status.ToString(),
            a.AcceptedAt,
            a.CompletedAt)).ToList();

        return Result<PagedResult<AssignmentDto>>.Success(new PagedResult<AssignmentDto>
        {
            Items = dtos,
            TotalCount = total,
            Page = req.Page,
            PageSize = req.PageSize
        });
    }
}
