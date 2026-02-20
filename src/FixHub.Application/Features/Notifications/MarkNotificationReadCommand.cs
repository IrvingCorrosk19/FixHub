using FixHub.Application.Common.Interfaces;
using FixHub.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FixHub.Application.Features.Notifications;

public record MarkNotificationReadCommand(Guid NotificationId, Guid UserId) : IRequest<Result<Unit>>;

public class MarkNotificationReadCommandHandler(IApplicationDbContext db)
    : IRequestHandler<MarkNotificationReadCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(MarkNotificationReadCommand req, CancellationToken ct)
    {
        var notification = await db.Notifications
            .FirstOrDefaultAsync(n => n.Id == req.NotificationId && n.UserId == req.UserId, ct);

        if (notification is null)
            return Result<Unit>.Failure("Notification not found.", "NOT_FOUND");

        notification.IsRead = true;
        await db.SaveChangesAsync(ct);
        return Result<Unit>.Success(Unit.Value);
    }
}
