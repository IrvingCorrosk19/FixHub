using FixHub.API.Extensions;
using FixHub.Application.Features.Admin;
using FixHub.Application.Features.Jobs;
using FixHub.Application.Features.Proposals;
using FixHub.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixHub.API.Controllers.v1;

[Authorize]
public class JobsController(ISender mediator) : ApiControllerBase
{
    /// <summary>Create a new job. [Customer only]</summary>
    [HttpPost]
    [Authorize(Policy = "CustomerOnly")]
    [ProducesResponseType(typeof(JobDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateJobRequest request,
        CancellationToken ct)
    {
        var command = new CreateJobCommand(
            CurrentUserId,
            request.CategoryId,
            request.Title,
            request.Description,
            request.AddressText,
            request.Lat,
            request.Lng,
            request.BudgetMin,
            request.BudgetMax);

        var result = await mediator.Send(command, ct);
        return result.ToActionResult(this, successStatusCode: 201);
    }

    /// <summary>Get a specific job by ID. [Customer: own only | Technician: assigned/open/has proposal | Admin: all]</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(JobDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetJobQuery(id, CurrentUserId, CurrentUserRole), ct);
        return result.ToActionResult(this);
    }

    /// <summary>List jobs with optional filters and pagination. [Technician/Admin only — Customer must use GET /mine]</summary>
    [HttpGet]
    [ProducesResponseType(typeof(FixHub.Application.Common.Models.PagedResult<JobDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [FromQuery] JobStatus? status,
        [FromQuery] int? categoryId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (CurrentUserRole == "Customer")
            return StatusCode(403, new ProblemDetails
            {
                Title = "Customers must use GET /api/v1/jobs/mine to list their requests.",
                Status = 403,
                Instance = HttpContext.Request.Path,
                Extensions = { ["errorCode"] = "FORBIDDEN" }
            });
        var result = await mediator.Send(new ListJobsQuery(status, categoryId, page, pageSize), ct);
        return result.ToActionResult(this);
    }

    /// <summary>List jobs created by the current customer.</summary>
    [HttpGet("mine")]
    [Authorize(Policy = "CustomerOnly")]
    [ProducesResponseType(typeof(FixHub.Application.Common.Models.PagedResult<JobDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Mine(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new ListMyJobsQuery(CurrentUserId, page, pageSize), ct);
        return result.ToActionResult(this);
    }

    /// <summary>Get proposals for a job. [Customer (own job) / Admin]</summary>
    [HttpGet("{id:guid}/proposals")]
    [ProducesResponseType(typeof(List<ProposalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetProposals(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(
            new GetJobProposalsQuery(id, CurrentUserId, IsAdmin), ct);
        return result.ToActionResult(this);
    }

    /// <summary>Submit a proposal for a job. [Technician only]</summary>
    [HttpPost("{id:guid}/proposals")]
    [Authorize(Policy = "TechnicianOnly")]
    [ProducesResponseType(typeof(ProposalDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SubmitProposal(
        Guid id,
        [FromBody] SubmitProposalRequest request,
        CancellationToken ct)
    {
        var command = new SubmitProposalCommand(id, CurrentUserId, request.Price, request.Message);
        var result = await mediator.Send(command, ct);
        return result.ToActionResult(this, successStatusCode: 201);
    }

    /// <summary>Mark a job as completed. [Customer only — own job]</summary>
    [HttpPost("{id:guid}/complete")]
    [Authorize(Policy = "CustomerOnly")]
    [ProducesResponseType(typeof(JobDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new CompleteJobCommand(id, CurrentUserId), ct);
        return result.ToActionResult(this);
    }

    /// <summary>Cancel a job. [Customer only — own job, only before InProgress]</summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = "CustomerOnly")]
    [ProducesResponseType(typeof(JobDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new CancelJobCommand(id, CurrentUserId), ct);
        return result.ToActionResult(this);
    }

    /// <summary>Report an issue on a job. [Customer (own job) or Admin]</summary>
    [HttpPost("{id:guid}/issues")]
    [ProducesResponseType(typeof(IssueDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ReportIssue(
        Guid id,
        [FromBody] ReportIssueRequest request,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new ReportJobIssueCommand(id, CurrentUserId, IsAdmin, request.Reason, request.Detail), ct);
        return result.ToActionResult(this, successStatusCode: 201);
    }
}

// ─── Request DTOs ─────────────────────────────────────────────────────────────
public record CreateJobRequest(
    int CategoryId,
    string Title,
    string Description,
    string AddressText,
    decimal? Lat,
    decimal? Lng,
    decimal? BudgetMin,
    decimal? BudgetMax
);

public record SubmitProposalRequest(decimal Price, string? Message);

public record ReportIssueRequest(string Reason, string? Detail);
