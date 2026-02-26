using FixHub.Application.Common.Interfaces;
using FixHub.Application.Common.Models;
using FixHub.Domain.Entities;
using FixHub.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FixHub.Application.Features.Admin;

public record ReassignJobCommand(
    Guid JobId,
    Guid AdminUserId,
    Guid ToTechnicianId,
    string Reason,
    string? ReasonDetail
) : IRequest<Result<ReassignJobResponse>>;

public record ReassignJobResponse(
    Guid JobId,
    Guid NewAssignmentId,
    Guid ToTechnicianId,
    Guid OverrideId
);

public class ReassignJobCommandValidator : AbstractValidator<ReassignJobCommand>
{
    public ReassignJobCommandValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required.")
            .MaximumLength(200).WithMessage("Reason cannot exceed 200 characters.");
        RuleFor(x => x.ReasonDetail)
            .MaximumLength(1000).WithMessage("ReasonDetail cannot exceed 1000 characters.");
    }
}

public class ReassignJobCommandHandler(
    IApplicationDbContext db,
    IAuditService audit)
    : IRequestHandler<ReassignJobCommand, Result<ReassignJobResponse>>
{
    public async Task<Result<ReassignJobResponse>> Handle(ReassignJobCommand req, CancellationToken ct)
    {
        var job = await db.Jobs
            .Include(j => j.Assignment)
            .ThenInclude(a => a!.Proposal)
            .ThenInclude(p => p!.Technician)
            .Include(j => j.Category)
            .Include(j => j.Customer)
            .FirstOrDefaultAsync(j => j.Id == req.JobId, ct);

        if (job is null)
            return Result<ReassignJobResponse>.Failure("Job not found.", "JOB_NOT_FOUND");

        if (job.Status == JobStatus.Completed || job.Status == JobStatus.Cancelled)
            return Result<ReassignJobResponse>.Failure(
                "Job cannot be reassigned when Completed or Cancelled.",
                "INVALID_STATUS");

        var currentAssignment = job.Assignment;
        if (currentAssignment is null)
            return Result<ReassignJobResponse>.Failure("Job has no current assignment to reassign.", "NO_ASSIGNMENT");

        var toUser = await db.Users
            .Include(u => u.TechnicianProfile)
            .FirstOrDefaultAsync(u => u.Id == req.ToTechnicianId, ct);

        if (toUser is null)
            return Result<ReassignJobResponse>.Failure("Target technician user not found.", "USER_NOT_FOUND");

        if (toUser.TechnicianProfile is null)
            return Result<ReassignJobResponse>.Failure("Target user is not a technician.", "NOT_TECHNICIAN");

        if (toUser.Id == currentAssignment.Proposal.TechnicianId)
            return Result<ReassignJobResponse>.Failure("Target technician is already assigned to this job.", "SAME_TECHNICIAN");

        var currentProposal = currentAssignment.Proposal;
        var fromTechnicianId = currentProposal.TechnicianId;
        var price = currentProposal.Price;
        var now = DateTime.UtcNow;

        var existingProposalForNewTech = await db.Proposals
            .FirstOrDefaultAsync(p => p.JobId == job.Id && p.TechnicianId == req.ToTechnicianId, ct);

        await using var transaction = await db.BeginTransactionAsync(ct);
        try
        {
            var overrideEntry = new AssignmentOverride
            {
                Id = Guid.NewGuid(),
                JobId = job.Id,
                FromTechnicianId = fromTechnicianId,
                ToTechnicianId = req.ToTechnicianId,
                Reason = req.Reason.Trim(),
                ReasonDetail = req.ReasonDetail?.Trim(),
                AdminUserId = req.AdminUserId,
                CreatedAtUtc = now
            };
            db.AssignmentOverrides.Add(overrideEntry);

            Proposal newProposal;
            if (existingProposalForNewTech is not null)
            {
                existingProposalForNewTech.Status = ProposalStatus.Accepted;
                existingProposalForNewTech.Price = price;
                newProposal = existingProposalForNewTech;
            }
            else
            {
                newProposal = new Proposal
                {
                    Id = Guid.NewGuid(),
                    JobId = job.Id,
                    TechnicianId = req.ToTechnicianId,
                    Price = price,
                    Message = "Reasignación manual",
                    Status = ProposalStatus.Accepted,
                    CreatedAt = now
                };
                db.Proposals.Add(newProposal);
            }

            db.JobAssignments.Remove(currentAssignment);

            var newAssignment = new JobAssignment
            {
                Id = Guid.NewGuid(),
                JobId = job.Id,
                ProposalId = newProposal.Id,
                AcceptedAt = now
            };
            db.JobAssignments.Add(newAssignment);

            job.AssignedAt = now;

            await db.SaveChangesAsync(ct);

            await audit.LogAsync(
                req.AdminUserId,
                "Job.Reassign",
                "Job",
                job.Id,
                new
                {
                    before = new { fromTechnicianId, assignmentId = currentAssignment.Id },
                    after = new { toTechnicianId = req.ToTechnicianId, newAssignmentId = newAssignment.Id },
                    overrideId = overrideEntry.Id
                },
                ct);

            await transaction.CommitAsync(ct);

            return Result<ReassignJobResponse>.Success(new ReassignJobResponse(
                job.Id,
                newAssignment.Id,
                req.ToTechnicianId,
                overrideEntry.Id));
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(ct);
            return Result<ReassignJobResponse>.Failure(
                "Concurrent modification detected. Please retry.",
                "CONCURRENCY_CONFLICT");
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}
