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
    DateTime CreatedAt,
    Guid? AssignedTechnicianId = null,
    string? AssignedTechnicianName = null,
    DateTime? AssignedAt = null,
    DateTime? StartedAt = null,
    DateTime? CompletedAt = null,
    DateTime? CancelledAt = null
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
public class CreateJobCommandHandler(IApplicationDbContext db, IDashboardCacheInvalidator dashboardCache, INotificationService notifications)
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
        dashboardCache.Invalidate();

        // Notificar a Cliente: solicitud recibida (FASE 13)
        await notifications.NotifyAsync(req.CustomerId, NotificationType.JobCreated,
            "Hemos recibido tu solicitud. Te asignaremos un técnico pronto.", job.Id, ct);

        // Notificar a Admins: nueva solicitud
        var adminIds = await db.Users
            .Where(u => u.Role == UserRole.Admin)
            .Select(u => u.Id)
            .ToListAsync(ct);
        if (adminIds.Count > 0)
            await notifications.NotifyManyAsync(adminIds, NotificationType.JobCreated,
                $"Nueva solicitud: {job.Title}", job.Id, ct);

        // Auto-asignar al técnico aprobado único (por el momento un solo técnico).
        var techProfile = await db.TechnicianProfiles
            .Include(tp => tp.User)
            .FirstOrDefaultAsync(tp => tp.Status == TechnicianStatus.Approved, ct);

        if (techProfile != null)
        {
            var price = req.BudgetMin ?? req.BudgetMax ?? 1m;
            var proposal = new Proposal
            {
                Id = Guid.NewGuid(),
                JobId = job.Id,
                TechnicianId = techProfile.UserId,
                Price = price,
                Message = "Asignación automática",
                Status = ProposalStatus.Accepted,
                CreatedAt = DateTime.UtcNow
            };
            db.Proposals.Add(proposal);

            db.JobAssignments.Add(new JobAssignment
            {
                Id = Guid.NewGuid(),
                JobId = job.Id,
                ProposalId = proposal.Id,
                AcceptedAt = DateTime.UtcNow
            });

            job.Status = JobStatus.Assigned;
            job.AssignedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            dashboardCache.Invalidate();

            // Notificar Customer + Technician por auto-asignación
            await notifications.NotifyAsync(req.CustomerId, NotificationType.JobAssigned,
                "Tu solicitud ha sido asignada a un técnico.", job.Id, ct);
            await notifications.NotifyAsync(techProfile.UserId, NotificationType.JobAssigned,
                $"Has sido asignado a: {job.Title}", job.Id, ct);

            return Result<JobDto>.Success(job.ToDto(
                customer.FullName, category.Name, techProfile.UserId, techProfile.User.FullName));
        }

        return Result<JobDto>.Success(job.ToDto(customer.FullName, category.Name));
    }
}

// ─── Extension ────────────────────────────────────────────────────────────────
public static class JobMappingExtensions
{
    public static JobDto ToDto(this Job job, string customerName, string categoryName,
        Guid? assignedTechnicianId = null, string? assignedTechnicianName = null) =>
        new(job.Id, job.CustomerId, customerName, job.CategoryId, categoryName,
            job.Title, job.Description, job.AddressText, job.Lat, job.Lng,
            job.Status.ToString(), job.BudgetMin, job.BudgetMax, job.CreatedAt,
            assignedTechnicianId, assignedTechnicianName,
            job.AssignedAt, job.Assignment?.StartedAt, job.CompletedAt, job.CancelledAt);
}
