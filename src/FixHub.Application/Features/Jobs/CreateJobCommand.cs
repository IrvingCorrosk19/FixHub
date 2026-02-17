using FixHub.Application.Common.Interfaces;
using FixHub.Application.Common.Models;
using FixHub.Domain.Entities;
using FixHub.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FixHub.Application.Features.Jobs;

// ─── Command ──────────────────────────────────────────────────────────────────
public record CreateJobCommand(
    Guid CustomerId,
    int CategoryId,
    string Title,
    string Description,
    string AddressText,
    decimal? Lat,
    decimal? Lng,
    decimal? BudgetMin,
    decimal? BudgetMax
) : IRequest<Result<JobDto>>;

// ─── Response ─────────────────────────────────────────────────────────────────
public record JobDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    int CategoryId,
    string CategoryName,
    string Title,
    string Description,
    string AddressText,
    decimal? Lat,
    decimal? Lng,
    string Status,
    decimal? BudgetMin,
    decimal? BudgetMax,
    DateTime CreatedAt
);

// ─── Validator ────────────────────────────────────────────────────────────────
public class CreateJobCommandValidator : AbstractValidator<CreateJobCommand>
{
    public CreateJobCommandValidator()
    {
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.AddressText).NotEmpty().MaximumLength(500);
        RuleFor(x => x.BudgetMin).GreaterThan(0).When(x => x.BudgetMin.HasValue);
        RuleFor(x => x.BudgetMax)
            .GreaterThanOrEqualTo(x => x.BudgetMin!.Value)
            .When(x => x.BudgetMin.HasValue && x.BudgetMax.HasValue)
            .WithMessage("BudgetMax must be >= BudgetMin.");
        RuleFor(x => x.Lat).InclusiveBetween(-90, 90).When(x => x.Lat.HasValue);
        RuleFor(x => x.Lng).InclusiveBetween(-180, 180).When(x => x.Lng.HasValue);
    }
}

// ─── Handler ──────────────────────────────────────────────────────────────────
public class CreateJobCommandHandler(IApplicationDbContext db)
    : IRequestHandler<CreateJobCommand, Result<JobDto>>
{
    public async Task<Result<JobDto>> Handle(CreateJobCommand req, CancellationToken ct)
    {
        var category = await db.ServiceCategories
            .FirstOrDefaultAsync(c => c.Id == req.CategoryId && c.IsActive, ct);

        if (category is null)
            return Result<JobDto>.Failure("Category not found or inactive.", "CATEGORY_NOT_FOUND");

        var customer = await db.Users
            .FirstOrDefaultAsync(u => u.Id == req.CustomerId, ct);

        if (customer is null)
            return Result<JobDto>.Failure("Customer not found.", "USER_NOT_FOUND");

        var job = new Job
        {
            Id = Guid.NewGuid(),
            CustomerId = req.CustomerId,
            CategoryId = req.CategoryId,
            Title = req.Title.Trim(),
            Description = req.Description.Trim(),
            AddressText = req.AddressText.Trim(),
            Lat = req.Lat,
            Lng = req.Lng,
            Status = JobStatus.Open,
            BudgetMin = req.BudgetMin,
            BudgetMax = req.BudgetMax,
            CreatedAt = DateTime.UtcNow
        };

        db.Jobs.Add(job);
        await db.SaveChangesAsync(ct);

        return Result<JobDto>.Success(job.ToDto(customer.FullName, category.Name));
    }
}

// ─── Extension ────────────────────────────────────────────────────────────────
public static class JobMappingExtensions
{
    public static JobDto ToDto(this Job job, string customerName, string categoryName) =>
        new(job.Id, job.CustomerId, customerName, job.CategoryId, categoryName,
            job.Title, job.Description, job.AddressText, job.Lat, job.Lng,
            job.Status.ToString(), job.BudgetMin, job.BudgetMax, job.CreatedAt);
}
