using FixHub.Application.Common.Interfaces;
using FixHub.Application.Common.Models;
using FixHub.Application.Features.Admin;
using FixHub.Application.Features.Auth;
using FixHub.Application.Features.Jobs;
using FixHub.Application.Features.Proposals;
using FixHub.Application.Features.Reviews;
using MediatR;

namespace FixHub.Application.Common.Behaviors;

/// <summary>
/// FASE 5.4: Registra eventos de auditoría tras ejecutar el handler. Sin PII (no email/password/tokens).
/// </summary>
public sealed class AuditBehavior<TRequest, TResponse>(
    IAuditService auditService)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next();

        try
        {
            await TryLogAuditAsync(request, response, cancellationToken);
        }
        catch
        {
            // No fallar la request por error de auditoría
        }

        return response;
    }

    private async Task TryLogAuditAsync(TRequest request, TResponse response, CancellationToken ct)
    {
        (Guid? actorUserId, string? action, string? entityType, Guid? entityId, object? metadata) audit = (request, response) switch
        {
            (RegisterCommand _, Result<AuthResponse> r) when r.IsSuccess && r.Value != null
                => (r.Value.UserId, "AUTH_REGISTER", "User", r.Value.UserId, new { role = r.Value.Role }),

            (LoginCommand _, Result<AuthResponse> r) when !r.IsSuccess
                => (null, "AUTH_LOGIN_FAIL", null, null, new { reason = r.ErrorCode ?? "INVALID_CREDENTIALS" }),

            (CreateJobCommand cmd, Result<JobDto> r) when r.IsSuccess && r.Value != null
                => (cmd.CustomerId, "JOB_CREATE", "Job", r.Value.Id, null),

            (SubmitProposalCommand cmd, Result<ProposalDto> r) when r.IsSuccess && r.Value != null
                => (cmd.TechnicianId, "PROPOSAL_SUBMIT", "Proposal", r.Value.Id, new { jobId = cmd.JobId }),

            (AcceptProposalCommand cmd, Result<AcceptProposalResponse> r) when r.IsSuccess && r.Value != null
                => (cmd.AcceptedByUserId, "PROPOSAL_ACCEPT", "Proposal", r.Value.ProposalId, new { jobId = r.Value.JobId }),

            (CompleteJobCommand cmd, Result<JobDto> r) when r.IsSuccess && r.Value != null
                => (cmd.CustomerId, "JOB_COMPLETE", "Job", cmd.JobId, new { jobId = cmd.JobId, statusAfter = r.Value.Status }),

            (CreateReviewCommand cmd, Result<ReviewDto> r) when r.IsSuccess && r.Value != null
                => (cmd.CustomerId, "REVIEW_CREATE", "Review", r.Value.Id, new { jobId = cmd.JobId }),

            (CancelJobCommand cmd, Result<JobDto> r) when r.IsSuccess
                => (cmd.CustomerId, "JOB_CANCEL", "Job", cmd.JobId, new { jobId = cmd.JobId, statusAfter = "Cancelled" }),

            (ReportJobIssueCommand cmd, Result<IssueDto> r) when r.IsSuccess && r.Value != null
                => (cmd.ReportedByUserId, "JOB_ISSUE_REPORT", "JobIssue", r.Value.Id, new { jobId = cmd.JobId, issueId = r.Value.Id, reason = cmd.Reason }),

            (StartJobCommand cmd, Result<JobDto> r) when r.IsSuccess
                => (cmd.AdminUserId, "JOB_START", "Job", cmd.JobId, new { jobId = cmd.JobId, statusAfter = "InProgress" }),

            (AdminUpdateJobStatusCommand cmd, Result<JobDto> r) when r.IsSuccess && r.Value != null
                => (cmd.AdminUserId, "JOB_ADMIN_UPDATE_STATUS", "Job", cmd.JobId, new { jobId = cmd.JobId, statusAfter = r.Value.Status }),

            _ => (null, null, null, null, null)
        };

        if (audit.action is not null)
            await auditService.LogAsync(audit.actorUserId, audit.action, audit.entityType, audit.entityId, audit.metadata, ct);
    }
}
