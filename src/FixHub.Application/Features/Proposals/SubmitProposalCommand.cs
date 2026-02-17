using FixHub.Application.Common.Interfaces;
using FixHub.Application.Common.Models;
using FixHub.Domain.Entities;
using FixHub.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FixHub.Application.Features.Proposals;

// ─── Command ──────────────────────────────────────────────────────────────────
public record SubmitProposalCommand(
    Guid JobId,
    Guid TechnicianId,
    decimal Price,
    string? Message
) : IRequest<Result<ProposalDto>>;

// ─── Response ─────────────────────────────────────────────────────────────────
public record ProposalDto(
    Guid Id,
    Guid JobId,
    Guid TechnicianId,
    string TechnicianName,
    decimal Price,
    string? Message,
    string Status,
    DateTime CreatedAt
);

// ─── Validator ────────────────────────────────────────────────────────────────
public class SubmitProposalCommandValidator : AbstractValidator<SubmitProposalCommand>
{
    public SubmitProposalCommandValidator()
    {
        RuleFor(x => x.Price).GreaterThan(0).LessThan(1_000_000);
        RuleFor(x => x.Message).MaximumLength(1000).When(x => x.Message != null);
    }
}

// ─── Handler ──────────────────────────────────────────────────────────────────
public class SubmitProposalCommandHandler(IApplicationDbContext db)
    : IRequestHandler<SubmitProposalCommand, Result<ProposalDto>>
{
    public async Task<Result<ProposalDto>> Handle(SubmitProposalCommand req, CancellationToken ct)
    {
        var job = await db.Jobs.FirstOrDefaultAsync(j => j.Id == req.JobId, ct);

        if (job is null)
            return Result<ProposalDto>.Failure("Job not found.", "JOB_NOT_FOUND");

        if (job.Status != JobStatus.Open)
            return Result<ProposalDto>.Failure(
                "Proposals can only be submitted for Open jobs.", "JOB_NOT_OPEN");

        // Técnico no puede ser el mismo que el Customer
        if (job.CustomerId == req.TechnicianId)
            return Result<ProposalDto>.Failure(
                "You cannot submit a proposal for your own job.", "SELF_PROPOSAL");

        // Verificar propuesta duplicada (antes de que la DB lo rechace)
        var duplicate = await db.Proposals
            .AnyAsync(p => p.JobId == req.JobId && p.TechnicianId == req.TechnicianId, ct);

        if (duplicate)
            return Result<ProposalDto>.Failure(
                "You already submitted a proposal for this job.", "DUPLICATE_PROPOSAL");

        var technician = await db.Users.FirstOrDefaultAsync(u => u.Id == req.TechnicianId, ct);
        if (technician is null)
            return Result<ProposalDto>.Failure("Technician not found.", "USER_NOT_FOUND");

        var proposal = new Proposal
        {
            Id = Guid.NewGuid(),
            JobId = req.JobId,
            TechnicianId = req.TechnicianId,
            Price = req.Price,
            Message = req.Message?.Trim(),
            Status = ProposalStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        db.Proposals.Add(proposal);
        await db.SaveChangesAsync(ct);

        return Result<ProposalDto>.Success(proposal.ToDto(technician.FullName));
    }
}

// ─── Extension ────────────────────────────────────────────────────────────────
public static class ProposalMappingExtensions
{
    public static ProposalDto ToDto(this Proposal p, string technicianName) =>
        new(p.Id, p.JobId, p.TechnicianId, technicianName,
            p.Price, p.Message, p.Status.ToString(), p.CreatedAt);
}
