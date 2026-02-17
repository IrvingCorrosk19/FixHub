using FixHub.Application.Common.Interfaces;
using FixHub.Application.Common.Models;
using FixHub.Domain.Entities;
using FixHub.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FixHub.Application.Features.Reviews;

// ─── Command ──────────────────────────────────────────────────────────────────
public record CreateReviewCommand(
    Guid JobId,
    Guid CustomerId,
    int Stars,
    string? Comment
) : IRequest<Result<ReviewDto>>;

// ─── Response ─────────────────────────────────────────────────────────────────
public record ReviewDto(
    Guid Id,
    Guid JobId,
    Guid TechnicianId,
    string TechnicianName,
    int Stars,
    string? Comment,
    DateTime CreatedAt
);

// ─── Validator ────────────────────────────────────────────────────────────────
public class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewCommandValidator()
    {
        RuleFor(x => x.Stars).InclusiveBetween(1, 5)
            .WithMessage("Stars must be between 1 and 5.");
        RuleFor(x => x.Comment).MaximumLength(1000).When(x => x.Comment != null);
    }
}

// ─── Handler ──────────────────────────────────────────────────────────────────
public class CreateReviewCommandHandler(IApplicationDbContext db)
    : IRequestHandler<CreateReviewCommand, Result<ReviewDto>>
{
    public async Task<Result<ReviewDto>> Handle(CreateReviewCommand req, CancellationToken ct)
    {
        var job = await db.Jobs
            .Include(j => j.Assignment)
            .FirstOrDefaultAsync(j => j.Id == req.JobId, ct);

        if (job is null)
            return Result<ReviewDto>.Failure("Job not found.", "JOB_NOT_FOUND");

        if (job.CustomerId != req.CustomerId)
            return Result<ReviewDto>.Failure("Only the job owner can leave a review.", "FORBIDDEN");

        if (job.Status != JobStatus.Completed)
            return Result<ReviewDto>.Failure(
                "Job must be Completed before leaving a review.", "JOB_NOT_COMPLETED");

        // Verificar review duplicado (antes del constraint DB)
        var exists = await db.Reviews.AnyAsync(r => r.JobId == req.JobId, ct);
        if (exists)
            return Result<ReviewDto>.Failure("A review already exists for this job.", "REVIEW_EXISTS");

        if (job.Assignment is null)
            return Result<ReviewDto>.Failure("No assignment found for this job.", "NO_ASSIGNMENT");

        // Obtener el TechnicianId desde la propuesta aceptada
        var proposal = await db.Proposals
            .FirstOrDefaultAsync(p => p.Id == job.Assignment.ProposalId, ct);

        if (proposal is null)
            return Result<ReviewDto>.Failure("Accepted proposal not found.", "PROPOSAL_NOT_FOUND");

        var technician = await db.Users.FirstOrDefaultAsync(u => u.Id == proposal.TechnicianId, ct);
        if (technician is null)
            return Result<ReviewDto>.Failure("Technician not found.", "USER_NOT_FOUND");

        var review = new Review
        {
            Id = Guid.NewGuid(),
            JobId = req.JobId,
            CustomerId = req.CustomerId,
            TechnicianId = proposal.TechnicianId,
            Stars = req.Stars,
            Comment = req.Comment?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        db.Reviews.Add(review);

        // Recalcular AvgRating del técnico
        var allStars = await db.Reviews
            .Where(r => r.TechnicianId == proposal.TechnicianId)
            .Select(r => r.Stars)
            .ToListAsync(ct);

        allStars.Add(req.Stars); // Incluir la nueva

        var techProfile = await db.TechnicianProfiles
            .FirstOrDefaultAsync(tp => tp.UserId == proposal.TechnicianId, ct);

        if (techProfile is not null)
            techProfile.AvgRating = (decimal)allStars.Average();

        await db.SaveChangesAsync(ct);

        return Result<ReviewDto>.Success(new ReviewDto(
            review.Id, review.JobId, review.TechnicianId,
            technician.FullName, review.Stars, review.Comment, review.CreatedAt));
    }
}
