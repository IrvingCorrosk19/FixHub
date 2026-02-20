using FixHub.Application.Common.Interfaces;
using FixHub.Application.Common.Models;
using FixHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FixHub.Application.Features.Notifications;

// ─── DTO ─────────────────────────────────────────────────────────────────────
public record NotificationDto(
    Guid Id,
    Guid? JobId,
    string Type,
    string Message,
    bool IsRead,
    DateTime CreatedAt
);

// ─── Query ────────────────────────────────────────────────────────────────────
public record GetMyNotificationsQuery(Guid UserId, int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedResult<NotificationDto>>>;

public class GetMyNotificationsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetMyNotificationsQuery, Result<PagedResult<NotificationDto>>>
{
    public async Task<Result<PagedResult<NotificationDto>>> Handle(GetMyNotificationsQuery req, CancellationToken ct)
    {
        var query = db.Notifications
            .Where(n => n.UserId == req.UserId)
            .OrderByDescending(n => n.CreatedAt);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(n => new NotificationDto(
                n.Id,
                n.JobId,
                n.Type.ToString(),
                n.Message,
                n.IsRead,
                n.CreatedAt))
            .ToListAsync(ct);

        return Result<PagedResult<NotificationDto>>.Success(new PagedResult<NotificationDto>
        {
            Items = items,
            TotalCount = total,
            Page = req.Page,
            PageSize = req.PageSize
        });
    }
}
