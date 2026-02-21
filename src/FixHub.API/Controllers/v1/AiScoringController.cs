using FixHub.API.Extensions;
using FixHub.Application.Features.Scoring;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixHub.API.Controllers.v1;

[Authorize(Policy = "AdminOnly")]
[ApiController]
[Route("api/v1/ai-scoring")]  // Ruta explícita: [controller] daría "aiscoring"
public class AiScoringController(ISender mediator) : ControllerBase
{
    /// <summary>
    /// Rank technicians with pending proposals using rule-based scoring v1.
    /// Score = (AvgRating*2) + (CompletedJobs*0.1) - (CancelRate*5) + (IsVerified ? 5 : 0)
    /// Saves ScoreSnapshot per technician for audit trail.
    /// </summary>
    [HttpPost("jobs/{jobId:guid}/rank-technicians")]
    [ProducesResponseType(typeof(List<TechnicianRankDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RankTechnicians(Guid jobId, CancellationToken ct)
    {
        var result = await mediator.Send(new RankTechniciansCommand(jobId), ct);
        return result.ToActionResult(this);
    }
}
