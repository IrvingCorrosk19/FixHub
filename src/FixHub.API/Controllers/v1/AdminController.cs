using FixHub.API.Extensions;
using FixHub.Application.Common.Models;
using FixHub.Application.Features.Admin;
using FixHub.Application.Features.Jobs;
using FixHub.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixHub.API.Controllers.v1;

[Authorize(Policy = "AdminOnly")]
[ApiController]
[Route("api/v1/admin")]
public class AdminController(ISender mediator) : ApiControllerBase
{
    /// <summary>List technician applicants (postulantes) with optional status filter.</summary>
    [HttpGet("applicants")]
    [ProducesResponseType(typeof(PagedResult<ApplicantDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListApplicants(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new ListApplicantsQuery(page, pageSize, status), ct);
        return result.ToActionResult(this);
    }

    /// <summary>Update technician approval status (Pending, InterviewScheduled, Approved, Rejected).</summary>
    [HttpPatch("technicians/{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateTechnicianStatus(
        Guid id,
        [FromBody] UpdateStatusRequest request,
        CancellationToken ct = default)
    {
        if (!Enum.IsDefined(typeof(TechnicianStatus), request.Status))
            return BadRequest(new ProblemDetails { Title = "Invalid status." });

        var result = await mediator.Send(
            new UpdateTechnicianStatusCommand(id, (TechnicianStatus)request.Status), ct);
        return result.ToActionResult(this, successStatusCode: 204);
    }

    /// <summary>List all job issues/incidents. [Admin only]</summary>
    [HttpGet("issues")]
    [ProducesResponseType(typeof(PagedResult<IssueDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListIssues(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new ListJobIssuesQuery(page, pageSize), ct);
        return result.ToActionResult(this);
    }

    /// <summary>Get operational dashboard data (KPIs, SLA alerts, recent jobs). [Admin only]</summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(OpsDashboardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard(CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetOpsDashboardQuery(), ct);
        return result.ToActionResult(this);
    }

    /// <summary>Force-start a job (Open/Assigned → InProgress). [Admin only]</summary>
    [HttpPost("jobs/{id:guid}/start")]
    [ProducesResponseType(typeof(JobDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartJob(Guid id, CancellationToken ct = default)
    {
        var result = await mediator.Send(new StartJobCommand(id, CurrentUserId), ct);
        return result.ToActionResult(this);
    }

    /// <summary>Get admin metrics (emails, SLA alerts, avg times). [Admin only]</summary>
    [HttpGet("metrics")]
    [ProducesResponseType(typeof(AdminMetricsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMetrics(CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetAdminMetricsQuery(), ct);
        return result.ToActionResult(this);
    }

    /// <summary>Force-update a job status from dashboard. [Admin only]</summary>
    [HttpPatch("jobs/{id:guid}/status")]
    [ProducesResponseType(typeof(JobDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateJobStatus(
        Guid id,
        [FromBody] AdminJobStatusRequest request,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new AdminUpdateJobStatusCommand(id, request.NewStatus, CurrentUserId), ct);
        return result.ToActionResult(this);
    }

    /// <summary>Resolve a SLA alert. [Admin only] — FASE 14</summary>
    [HttpPatch("alerts/{id:guid}/resolve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ResolveAlert(Guid id, CancellationToken ct = default)
    {
        var result = await mediator.Send(new ResolveJobAlertCommand(id, CurrentUserId), ct);
        return result.ToActionResult(this, successStatusCode: 204);
    }

    /// <summary>Resolve a job issue/incident. [Admin only] — FASE 14</summary>
    [HttpPost("issues/{id:guid}/resolve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ResolveIssue(
        Guid id,
        [FromBody] ResolveIssueRequest request,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new ResolveJobIssueCommand(id, CurrentUserId, request.ResolutionNote), ct);
        return result.ToActionResult(this, successStatusCode: 204);
    }
}

public record UpdateStatusRequest(int Status);
public record AdminJobStatusRequest(string NewStatus);
public record ResolveIssueRequest(string ResolutionNote);
