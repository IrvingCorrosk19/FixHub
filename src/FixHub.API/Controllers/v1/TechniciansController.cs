using FixHub.API.Extensions;
using FixHub.Application.Common.Models;
using FixHub.Application.Features.Technicians;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixHub.API.Controllers.v1;

[Authorize]
public class TechniciansController(ISender mediator) : ApiControllerBase
{
    /// <summary>Get a technician's public profile.</summary>
    [HttpGet("{id:guid}/profile")]
    [ProducesResponseType(typeof(TechnicianProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetTechnicianProfileQuery(id), ct);
        return result.ToActionResult(this);
    }

    /// <summary>Get assignments for the current technician.</summary>
    [HttpGet("me/assignments")]
    [Authorize(Policy = "TechnicianOnly")]
    [ProducesResponseType(typeof(PagedResult<AssignmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MyAssignments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetMyAssignmentsQuery(CurrentUserId, page, pageSize), ct);
        return result.ToActionResult(this);
    }
}
