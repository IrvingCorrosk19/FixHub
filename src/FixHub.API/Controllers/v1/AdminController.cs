using FixHub.API.Extensions;
using FixHub.Application.Common.Models;
using FixHub.Application.Features.Admin;
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
}

public record UpdateStatusRequest(int Status);
