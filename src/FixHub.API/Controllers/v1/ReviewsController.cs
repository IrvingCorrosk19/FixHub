using FixHub.API.Extensions;
using FixHub.Application.Features.Reviews;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixHub.API.Controllers.v1;

[Authorize]
public class ReviewsController(ISender mediator) : ApiControllerBase
{
    /// <summary>Submit a review for a completed job. [Customer only]</summary>
    [HttpPost]
    [Authorize(Policy = "CustomerOnly")]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateReviewRequest request,
        CancellationToken ct)
    {
        var command = new CreateReviewCommand(
            request.JobId, CurrentUserId, request.Stars, request.Comment);

        var result = await mediator.Send(command, ct);
        return result.ToActionResult(this, successStatusCode: 201);
    }
}

public record CreateReviewRequest(Guid JobId, int Stars, string? Comment);
