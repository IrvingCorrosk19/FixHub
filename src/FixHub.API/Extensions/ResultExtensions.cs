using FixHub.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace FixHub.API.Extensions;

public static class ResultExtensions
{
    /// <summary>
    /// Convierte un Result&lt;T&gt; a IActionResult:
    ///   IsSuccess → statusCode (default 200)
    ///   !IsSuccess → ProblemDetails 400/404/409 según ErrorCode
    /// </summary>
    public static IActionResult ToActionResult<T>(
        this Result<T> result,
        ControllerBase controller,
        int successStatusCode = 200)
    {
        if (result.IsSuccess)
            return successStatusCode == 201
                ? controller.StatusCode(201, result.Value)
                : controller.Ok(result.Value);

        return result.ErrorCode switch
        {
            "JOB_NOT_FOUND"
            or "PROPOSAL_NOT_FOUND"
            or "PROFILE_NOT_FOUND"
            or "USER_NOT_FOUND"
            or "CATEGORY_NOT_FOUND"
            or "NO_ASSIGNMENT"
            or "NO_PROPOSALS"
                => controller.NotFound(ProblemFrom(result, 404)),

            "FORBIDDEN"
                => controller.StatusCode(403, ProblemFrom(result, 403)),

            "EMAIL_TAKEN"
            or "DUPLICATE_PROPOSAL"
            or "JOB_ALREADY_ASSIGNED"
            or "REVIEW_EXISTS"
                => controller.Conflict(ProblemFrom(result, 409)),

            "INVALID_CREDENTIALS"
                => controller.Unauthorized(ProblemFrom(result, 401)),

            _ => controller.BadRequest(ProblemFrom(result, 400))
        };
    }

    private static ProblemDetails ProblemFrom<T>(Result<T> result, int status) =>
        new()
        {
            Title = result.Error,
            Status = status,
            Extensions = { ["errorCode"] = result.ErrorCode }
        };
}
