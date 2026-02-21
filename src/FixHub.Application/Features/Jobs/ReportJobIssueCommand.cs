using FixHub.Application.Common.Interfaces;
using FixHub.Application.Common.Models;
using FixHub.Domain.Entities;
using FixHub.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FixHub.Application.Features.Jobs;

// ─── Command ──────────────────────────────────────────────────────────────────
public record ReportJobIssueCommand(
    Guid JobId,
    Guid ReportedByUserId,
    bool IsAdmin,
    string Reason,
    string? Detail
) : IRequest<Result<IssueDto>>;

// ─── Validator (FASE 8: Reason whitelist, Detail max length) ───────────────────
public class ReportJobIssueCommandValidator : AbstractValidator<ReportJobIssueCommand>
{
    private static readonly string[] ValidReasons = ["no_contact", "late", "bad_service", "other"];

    public ReportJobIssueCommandValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required.")
            .Must(r => ValidReasons.Contains(r?.Trim() ?? ""))
            .WithMessage($"Reason must be one of: {string.Join(", ", ValidReasons)}.");
        RuleFor(x => x.Detail)
            .MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Detail))
            .WithMessage("Detail cannot exceed 500 characters.");
    }
}

// ─── DTO ──────────────────────────────────────────────────────────────────────
// FASE 14: ResolvedAt, ResolvedByUserId, ResolutionNote son opcionales (backward-compatible).
public record IssueDto(
    Guid Id,
    Guid JobId,
    string JobTitle,
    string ReportedByName,
    string Reason,
    string? Detail,
    DateTime CreatedAt,
    DateTime? ResolvedAt = null,
    Guid? ResolvedByUserId = null,
    string? ResolutionNote = null
);

// ─── Handler ──────────────────────────────────────────────────────────────────
public class ReportJobIssueCommandHandler(IApplicationDbContext db, IDashboardCacheInvalidator dashboardCache, INotificationService notifications)
    : IRequestHandler<ReportJobIssueCommand, Result<IssueDto>>
{
    public async Task<Result<IssueDto>> Handle(ReportJobIssueCommand req, CancellationToken ct)
    {
        var job = await db.Jobs
            .Include(j => j.Customer)
            .FirstOrDefaultAsync(j => j.Id == req.JobId, ct);

        if (job is null)
            return Result<IssueDto>.Failure("Job not found.", "JOB_NOT_FOUND");

        // Solo el dueño del job o un Admin puede reportar
        if (!req.IsAdmin && job.CustomerId != req.ReportedByUserId)
            return Result<IssueDto>.Failure("Only the job owner can report issues.", "FORBIDDEN");

        var reporter = await db.Users
            .FirstOrDefaultAsync(u => u.Id == req.ReportedByUserId, ct);

        var issue = new JobIssue
        {
            Id = Guid.NewGuid(),
            JobId = req.JobId,
            ReportedByUserId = req.ReportedByUserId,
            Reason = req.Reason.Trim(),
            Detail = string.IsNullOrWhiteSpace(req.Detail) ? null : req.Detail.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        db.JobIssues.Add(issue);
        await db.SaveChangesAsync(ct);
        dashboardCache.Invalidate();

        var adminIds = await db.Users.Where(u => u.Role == UserRole.Admin).Select(u => u.Id).ToListAsync(ct);
        if (adminIds.Count > 0)
            await notifications.NotifyManyAsync(adminIds, NotificationType.IssueReported,
                $"Incidencia reportada en: {job.Title}", job.Id, ct);

        return Result<IssueDto>.Success(new IssueDto(
            issue.Id,
            job.Id,
            job.Title,
            reporter?.FullName ?? "Cliente",
            issue.Reason,
            issue.Detail,
            issue.CreatedAt));
    }
}
