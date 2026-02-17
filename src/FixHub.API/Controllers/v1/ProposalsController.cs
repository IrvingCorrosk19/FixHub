using FixHub.API.Extensions;
using FixHub.Application.Features.Proposals;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixHub.API.Controllers.v1;

[Authorize]
public class ProposalsController(ISender mediator) : ApiControllerBase
{
    /// <summary>Accept a proposal, creating a JobAssignment. [Customer only]</summary>
    [HttpPost("{id:guid}/accept")]
    [Authorize(Policy = "CustomerOnly")]
    [ProducesResponseType(typeof(AcceptProposalResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Accept(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new AcceptProposalCommand(id, CurrentUserId), ct);
        return result.ToActionResult(this);
    }
}
