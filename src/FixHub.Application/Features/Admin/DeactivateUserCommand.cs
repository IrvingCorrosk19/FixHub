using FixHub.Application.Common.Interfaces;
using FixHub.Application.Common.Models;
using FixHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FixHub.Application.Features.Admin;

public record DeactivateUserCommand(Guid UserId, Guid ActorUserId) : IRequest<Result<Unit>>;

public class DeactivateUserCommandHandler(
    IApplicationDbContext db,
    IAuditService audit)
    : IRequestHandler<DeactivateUserCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(DeactivateUserCommand req, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == req.UserId, ct);
        if (user is null)
            return Result<Unit>.Failure("User not found.", "USER_NOT_FOUND");

        var previousIsActive = user.IsActive;
        var previousIsSuspended = user.IsSuspended;
        var now = DateTime.UtcNow;

        user.IsActive = false;
        user.DeactivatedAt = now;
        user.RowVersion = Guid.NewGuid().ToByteArray();

        db.UserStatusHistories.Add(new UserStatusHistory
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            PreviousIsActive = previousIsActive,
            PreviousIsSuspended = previousIsSuspended,
            NewIsActive = false,
            NewIsSuspended = user.IsSuspended,
            Reason = "Deactivate",
            ActorUserId = req.ActorUserId,
            CreatedAtUtc = now
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
            "User.Deactivate",
            "User",
            user.Id,
            new { before = new { previousIsActive, previousIsSuspended }, after = new { user.IsActive, user.DeactivatedAt } },
            ct);

        return Result<Unit>.Success(Unit.Value);
    }
}
