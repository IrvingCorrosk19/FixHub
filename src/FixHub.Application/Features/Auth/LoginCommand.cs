using FixHub.Application.Common.Interfaces;
using FixHub.Application.Common.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FixHub.Application.Features.Auth;

// ─── Command ──────────────────────────────────────────────────────────────────
public record LoginCommand(string Email, string Password) : IRequest<Result<AuthResponse>>;

// ─── Validator ────────────────────────────────────────────────────────────────
public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

// ─── Handler ──────────────────────────────────────────────────────────────────
public class LoginCommandHandler(
    IApplicationDbContext db,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService
) : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), ct);

        if (user is null || !user.IsActive)
            return Result<AuthResponse>.Failure("Invalid credentials.", "INVALID_CREDENTIALS");

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
            return Result<AuthResponse>.Failure("Invalid credentials.", "INVALID_CREDENTIALS");

        var token = jwtTokenService.GenerateToken(user);

        return Result<AuthResponse>.Success(new AuthResponse(
            user.Id, user.Email, user.FullName, user.Role.ToString(), token));
    }
}
