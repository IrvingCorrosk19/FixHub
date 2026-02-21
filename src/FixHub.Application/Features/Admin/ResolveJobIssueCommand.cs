using FixHub.Application.Common.Interfaces;
using FixHub.Application.Common.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FixHub.Application.Features.Admin;

// ─── Command ──────────────────────────────────────────────────────────────────
/// <summary>FASE 14: Resuelve una incidencia de trabajo. Solo Admin. No permite doble resolución.</summary>
public record ResolveJobIssueCommand(
    Guid IssueId,
    Guid ResolvedByUserId,
    string ResolutionNote
) : IRequest<Result<object>>;

// ─── Validator ────────────────────────────────────────────────────────────────
public class ResolveJobIssueCommandValidator : AbstractValidator<ResolveJobIssueCommand>
{
    public ResolveJobIssueCommandValidator()
    {
        RuleFor(x => x.ResolutionNote)
            .NotEmpty().WithMessage("Resolution note is required.")
            .MaximumLength(1000).WithMessage("Resolution note cannot exceed 1000 characters.");
    }
}

// ─── Handler ──────────────────────────────────────────────────────────────────
public class ResolveJobIssueCommandHandler(IApplicationDbContext db, IDashboardCacheInvalidator dashboardCache)
    : IRequestHandler<ResolveJobIssueCommand, Result<object>>
{
    public async Task<Result<object>> Handle(ResolveJobIssueCommand req, CancellationToken ct)
    {
        var issue = await db.JobIssues
            .FirstOrDefaultAsync(i => i.Id == req.IssueId, ct);

        if (issue is null)
            return Result<object>.Failure("Issue not found.", "NOT_FOUND");

        if (issue.ResolvedAt.HasValue)
            return Result<object>.Failure("This issue has already been resolved.", "ALREADY_RESOLVED");

        issue.ResolvedAt = DateTime.UtcNow;
        issue.ResolvedByUserId = req.ResolvedByUserId;
        issue.ResolutionNote = req.ResolutionNote.Trim();

        await db.SaveChangesAsync(ct);
        dashboardCache.Invalidate();

        return Result<object>.Success(null!);
    }
}
