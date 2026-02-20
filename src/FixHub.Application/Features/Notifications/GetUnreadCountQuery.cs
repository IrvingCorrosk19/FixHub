using FixHub.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FixHub.Application.Features.Notifications;

public record GetUnreadCountQuery(Guid UserId) : IRequest<int>;

public class GetUnreadCountQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetUnreadCountQuery, int>
{
    public async Task<int> Handle(GetUnreadCountQuery req, CancellationToken ct)
    {
        return await db.Notifications
            .CountAsync(n => n.UserId == req.UserId && !n.IsRead, ct);
    }
}
