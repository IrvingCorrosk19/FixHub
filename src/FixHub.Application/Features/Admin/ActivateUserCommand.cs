using FixHub.Application.Common.Interfaces;
using FixHub.Application.Common.Models;
using FixHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FixHub.Application.Features.Admin;

public record ActivateUserCommand(Guid UserId, Guid ActorUserId) : IRequest<Result<Unit>>;

public class ActivateUserCommandHandler(
    IApplicationDbContext db,
    IAuditService audit)
    : IRequestHandler<ActivateUserCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(ActivateUserCommand req, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == req.UserId, ct);
        if (user is null)
            return Result<Unit>.Failure("User not found.", "USER_NOT_FOUND");

        var previousIsActive = user.IsActive;
        var previousIsSuspended = user.IsSuspended;

        user.IsActive = true;
        user.DeactivatedAt = null;
        user.RowVersion = Guid.NewGuid().ToByteArray();

        db.UserStatusHistories.Add(new UserStatusHistory
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            PreviousIsActive = previousIsActive,
            PreviousIsSuspended = previousIsSuspended,
            NewIsActive = true,
            NewIsSuspended = user.IsSuspended,
            Reason = "Activate",
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
            "User.Activate",
            "User",
            user.Id,
            new { before = new { previousIsActive, previousIsSuspended }, after = new { user.IsActive, user.DeactivatedAt } },
            ct);

        return Result<Unit>.Success(Unit.Value);
    }
}
