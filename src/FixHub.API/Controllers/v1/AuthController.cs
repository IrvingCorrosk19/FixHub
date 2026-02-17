using FixHub.API.Extensions;
using FixHub.Application.Features.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FixHub.API.Controllers.v1;

[EnableRateLimiting("AuthPolicy")]
public class AuthController(ISender mediator) : ApiControllerBase
{
    /// <summary>Register a new user (Customer, Technician, or Admin).</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken ct)
    {
        var command = new RegisterCommand(
            request.FullName, request.Email, request.Password, request.Role, request.Phone);

        var result = await mediator.Send(command, ct);
        return result.ToActionResult(this, successStatusCode: 201);
    }

    /// <summary>Authenticate and receive a JWT token.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct)
    {
        var result = await mediator.Send(new LoginCommand(request.Email, request.Password), ct);
        return result.ToActionResult(this);
    }
}

// ─── Request DTOs (definidos en el controller por ser solo API boundary) ───────
public record RegisterRequest(
    string FullName,
    string Email,
    string Password,
    FixHub.Domain.Enums.UserRole Role,
    string? Phone
);

public record LoginRequest(string Email, string Password);
