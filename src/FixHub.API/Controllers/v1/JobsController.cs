using FixHub.API.Extensions;
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

    /// <summary>Get a specific job by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(JobDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetJobQuery(id), ct);
        return result.ToActionResult(this);
    }

    /// <summary>List jobs with optional filters and pagination.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(FixHub.Application.Common.Models.PagedResult<JobDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] JobStatus? status,
        [FromQuery] int? categoryId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
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
