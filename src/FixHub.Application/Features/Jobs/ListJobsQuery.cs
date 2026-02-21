using FixHub.Application.Common.Interfaces;
using FixHub.Application.Common.Models;
using FixHub.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FixHub.Application.Features.Jobs;

// ─── Query ────────────────────────────────────────────────────────────────────
// FASE 14: Agregados RequesterId y RequesterRole para defensa en profundidad.
// No depender solo del Controller para el filtrado por rol.
public record ListJobsQuery(
    JobStatus? Status,
    int? CategoryId,
    int Page = 1,
    int PageSize = 20,
    Guid? RequesterId = null,
    string? RequesterRole = null
) : IRequest<Result<PagedResult<JobDto>>>;

public class ListJobsQueryValidator : AbstractValidator<ListJobsQuery>
{
    public ListJobsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

// ─── Handler ──────────────────────────────────────────────────────────────────
public class ListJobsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<ListJobsQuery, Result<PagedResult<JobDto>>>
{
    public async Task<Result<PagedResult<JobDto>>> Handle(ListJobsQuery req, CancellationToken ct)
    {
        var query = db.Jobs
            .Include(j => j.Customer)
            .Include(j => j.Category)
            .AsQueryable();

        // FASE 14: Filtrado por rol en el handler (defensa en profundidad).
        // Si el cliente pasa role=Customer, solo ve sus propios jobs.
        // Si pasa role=Technician, solo ve jobs abiertos o donde tiene propuesta.
        // Admin o sin rol → sin filtro adicional (ve todos).
        if (req.RequesterRole == "Customer" && req.RequesterId.HasValue)
        {
            query = query.Where(j => j.CustomerId == req.RequesterId.Value);
        }
        else if (req.RequesterRole == "Technician" && req.RequesterId.HasValue)
        {
            var techId = req.RequesterId.Value;
            query = query.Where(j =>
                j.Status == JobStatus.Open ||
                j.Proposals.Any(p => p.TechnicianId == techId));
        }
        // Admin: sin filtro adicional

        if (req.Status.HasValue)
            query = query.Where(j => j.Status == req.Status.Value);

        if (req.CategoryId.HasValue)
            query = query.Where(j => j.CategoryId == req.CategoryId.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(j => j.CreatedAt)
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
