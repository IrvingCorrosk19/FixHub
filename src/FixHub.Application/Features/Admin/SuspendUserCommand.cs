using FixHub.Application.Common.Interfaces;
using FixHub.Application.Common.Models;
using FixHub.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FixHub.Application.Features.Admin;

public record SuspendUserCommand(
    Guid UserId,
    Guid ActorUserId,
    DateTime? SuspendedUntil,
    string? SuspensionReason
) : IRequest<Result<Unit>>;

public class SuspendUserCommandValidator : AbstractValidator<SuspendUserCommand>
{
    public SuspendUserCommandValidator()
    {
        RuleFor(x => x.SuspensionReason)
            .MaximumLength(500).When(x => x.SuspensionReason != null);
    }
}

public class SuspendUserCommandHandler(
    IApplicationDbContext db,
    IAuditService audit)
    : IRequestHandler<SuspendUserCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(SuspendUserCommand req, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == req.UserId, ct);
        if (user is null)
            return Result<Unit>.Failure("User not found.", "USER_NOT_FOUND");

        var previousIsActive = user.IsActive;
        var previousIsSuspended = user.IsSuspended;

        user.IsSuspended = true;
        user.SuspendedUntil = req.SuspendedUntil;
        user.SuspensionReason = req.SuspensionReason?.Trim();
        user.RowVersion = Guid.NewGuid().ToByteArray();

        db.UserStatusHistories.Add(new UserStatusHistory
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            PreviousIsActive = previousIsActive,
            PreviousIsSuspended = previousIsSuspended,
            NewIsActive = user.IsActive,
            NewIsSuspended = true,
            Reason = req.SuspensionReason,
            ActorUserId = req.ActorUserId,
            CreatedAtUtc = DateTime.UtcNow
        });

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            return Result<Unit>.Failure("The user was modified by another process. Please refresh and try again.", "CONCURRENCY_CONFLICT");
        }

        await audit.LogAsync(
            req.ActorUserId,
            "User.Suspend",
            "User",
            user.Id,
            new { before = new { previousIsActive, previousIsSuspended }, after = new { user.IsActive, user.IsSuspended, user.SuspendedUntil, user.SuspensionReason } },
            ct);

        return Result<Unit>.Success(Unit.Value);
    }
}
