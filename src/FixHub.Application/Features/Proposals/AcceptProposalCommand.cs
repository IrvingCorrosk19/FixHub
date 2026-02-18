using FixHub.Application.Common.Interfaces;
using FixHub.Application.Common.Models;
using FixHub.Domain.Entities;
using FixHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FixHub.Application.Features.Proposals;

// ─── Command ──────────────────────────────────────────────────────────────────
public record AcceptProposalCommand(Guid ProposalId, Guid AcceptedByUserId, bool AcceptAsAdmin = false)
    : IRequest<Result<AcceptProposalResponse>>;

public record AcceptProposalResponse(
    Guid AssignmentId,
    Guid JobId,
    Guid ProposalId,
    Guid TechnicianId,
    string TechnicianName,
    decimal AcceptedPrice,
    DateTime AcceptedAt
);

// ─── Handler ──────────────────────────────────────────────────────────────────
public class AcceptProposalCommandHandler(IApplicationDbContext db)
    : IRequestHandler<AcceptProposalCommand, Result<AcceptProposalResponse>>
{
    public async Task<Result<AcceptProposalResponse>> Handle(
        AcceptProposalCommand req, CancellationToken ct)
    {
        var proposal = await db.Proposals
            .Include(p => p.Job)
            .Include(p => p.Technician)
            .FirstOrDefaultAsync(p => p.Id == req.ProposalId, ct);

        if (proposal is null)
            return Result<AcceptProposalResponse>.Failure("Proposal not found.", "PROPOSAL_NOT_FOUND");

        if (!req.AcceptAsAdmin && proposal.Job.CustomerId != req.AcceptedByUserId)
            return Result<AcceptProposalResponse>.Failure(
                "Solo el dueño del trabajo o un administrador pueden aceptar propuestas.", "FORBIDDEN");

        if (proposal.Status != ProposalStatus.Pending)
            return Result<AcceptProposalResponse>.Failure(
                $"Proposal is no longer pending. Status: {proposal.Status}", "PROPOSAL_NOT_PENDING");

        if (proposal.Job.Status != JobStatus.Open)
            return Result<AcceptProposalResponse>.Failure(
                "Job is no longer open for acceptance.", "JOB_NOT_OPEN");

        // Verificar que no exista ya un JobAssignment (doble check aparte del UNIQUE DB)
        var existingAssignment = await db.JobAssignments
            .AnyAsync(a => a.JobId == proposal.JobId, ct);

        if (existingAssignment)
            return Result<AcceptProposalResponse>.Failure(
                "This job already has an assigned technician.", "JOB_ALREADY_ASSIGNED");

        var now = DateTime.UtcNow;

        // Aceptar esta propuesta
        proposal.Status = ProposalStatus.Accepted;

        // Rechazar todas las demás propuestas del mismo job
        var otherProposals = await db.Proposals
            .Where(p => p.JobId == proposal.JobId
                     && p.Id != proposal.Id
                     && p.Status == ProposalStatus.Pending)
            .ToListAsync(ct);

        foreach (var other in otherProposals)
            other.Status = ProposalStatus.Rejected;

        // Crear JobAssignment
        var assignment = new JobAssignment
        {
            Id = Guid.NewGuid(),
            JobId = proposal.JobId,
            ProposalId = proposal.Id,
            AcceptedAt = now
        };

        db.JobAssignments.Add(assignment);

        // Cambiar status del Job a Assigned
        proposal.Job.Status = JobStatus.Assigned;

        await db.SaveChangesAsync(ct);

        return Result<AcceptProposalResponse>.Success(new AcceptProposalResponse(
            assignment.Id,
            assignment.JobId,
            assignment.ProposalId,
            proposal.TechnicianId,
            proposal.Technician.FullName,
            proposal.Price,
            assignment.AcceptedAt
        ));
    }
}
