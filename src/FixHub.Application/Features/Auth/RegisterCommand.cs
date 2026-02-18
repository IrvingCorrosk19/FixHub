using FixHub.Application.Common.Interfaces;
using FixHub.Application.Common.Models;
using FixHub.Domain.Entities;
using FixHub.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FixHub.Application.Features.Auth;

// ─── Command ──────────────────────────────────────────────────────────────────
public record RegisterCommand(
    string FullName,
    string Email,
    string Password,
    UserRole Role,
    string? Phone
) : IRequest<Result<AuthResponse>>;

// ─── Response ─────────────────────────────────────────────────────────────────
public record AuthResponse(Guid UserId, string Email, string FullName, string Role, string Token);

// ─── Validator ────────────────────────────────────────────────────────────────
public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128)
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.");
        RuleFor(x => x.Role).IsInEnum().Must(r => r != 0)
            .WithMessage("Role must be Customer, Technician, or Admin.");
        RuleFor(x => x.Phone).MaximumLength(30).When(x => x.Phone != null);
    }
}

// ─── Handler ──────────────────────────────────────────────────────────────────
public class RegisterCommandHandler(
    IApplicationDbContext db,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService
) : IRequestHandler<RegisterCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(RegisterCommand request, CancellationToken ct)
    {
        // Validar email único antes de intentar insertar
        var emailExists = await db.Users
            .AnyAsync(u => u.Email == request.Email.ToLowerInvariant(), ct);

        if (emailExists)
            return Result<AuthResponse>.Failure("Email already registered.", "EMAIL_TAKEN");

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Email = request.Email.ToLowerInvariant().Trim(),
            PasswordHash = passwordHasher.Hash(request.Password),
            Role = request.Role,
            Phone = request.Phone?.Trim(),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        db.Users.Add(user);

        // Si es Technician, crear perfil con estado Pending (reclutamiento)
        if (request.Role == UserRole.Technician)
        {
            db.TechnicianProfiles.Add(new TechnicianProfile
            {
                UserId = user.Id,
                Status = TechnicianStatus.Pending
            });
        }

        await db.SaveChangesAsync(ct);

        var token = jwtTokenService.GenerateToken(user);

        return Result<AuthResponse>.Success(new AuthResponse(
            user.Id, user.Email, user.FullName, user.Role.ToString(), token));
    }
}
