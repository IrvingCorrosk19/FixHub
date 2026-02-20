using FixHub.Application.Common.Interfaces;
using FixHub.Domain.Entities;
using FixHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FixHub.Infrastructure.Services;

/// <summary>
/// FASE 13: Notificaciones internas + enqueue de emails profesionales en Outbox.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IApplicationDbContext _db;
    private readonly IEmailOutboxService _emailOutbox;
    private readonly INotificationEmailComposer _emailComposer;

    public NotificationService(
        IApplicationDbContext db,
        IEmailOutboxService emailOutbox,
        INotificationEmailComposer emailComposer)
    {
        _db = db;
        _emailOutbox = emailOutbox;
        _emailComposer = emailComposer;
    }

    public async Task NotifyAsync(Guid userId, NotificationType type, string message, Guid? jobId = null, CancellationToken ct = default)
    {
        await NotifyManyAsync([userId], type, message, jobId, ct);
    }

    public async Task NotifyManyAsync(IEnumerable<Guid> userIds, NotificationType type, string message, Guid? jobId = null, CancellationToken ct = default)
    {
        var list = userIds.Distinct().ToList();
        if (list.Count == 0) return;

        string? jobTitle = null;
        string? categoryName = null;
        string? addressText = null;
        Guid? jobCustomerId = null;
        string? technicianName = null;
        string? technicianPhone = null;

        if (jobId.HasValue)
        {
            var job = await _db.Jobs
                .AsNoTracking()
                .Include(j => j.Category)
                .Include(j => j.Assignment)
                    .ThenInclude(a => a!.Proposal)
                    .ThenInclude(p => p!.Technician)
                .FirstOrDefaultAsync(j => j.Id == jobId.Value, ct);

            jobTitle = job?.Title;
            categoryName = job?.Category?.Name;
            addressText = job?.AddressText;
            jobCustomerId = job?.CustomerId;

            if ((type == NotificationType.JobAssigned || type == NotificationType.JobStarted) && job?.Assignment?.Proposal?.Technician != null)
            {
                var tech = job.Assignment.Proposal.Technician;
                technicianName = tech.FullName;
                technicianPhone = tech.Phone;
            }
        }

        var users = await _db.Users
            .AsNoTracking()
            .Where(u => list.Contains(u.Id))
            .Select(u => new { u.Id, u.Email, u.FullName })
            .ToListAsync(ct);

        var notifications = new List<Notification>();

        foreach (var uid in list)
        {
            var user = users.FirstOrDefault(u => u.Id == uid);
            var userName = user?.FullName ?? "Usuario";
            var msg = message.Length > 500 ? message[..500] : message;
            var notifId = Guid.NewGuid();

            notifications.Add(new Notification
            {
                Id = notifId,
                UserId = uid,
                JobId = jobId,
                Type = type,
                Message = msg,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            // FASE 13: Enqueue email profesional — solo cliente para Job*; admin para IssueReported/SlaAlert
            var shouldEmail = type switch
            {
                NotificationType.JobCreated => uid == jobCustomerId,
                NotificationType.JobAssigned or NotificationType.JobStarted or
                NotificationType.JobCompleted or NotificationType.JobCancelled => uid == jobCustomerId,
                NotificationType.IssueReported or NotificationType.SlaAlert => true,
                _ => false
            };

            if (shouldEmail && !string.IsNullOrWhiteSpace(user?.Email) && jobId.HasValue && jobTitle != null)
            {
                var eventType = (type == NotificationType.JobCreated && uid == jobCustomerId)
                    ? "JobReceived"
                    : type.ToString();

                var composition = _emailComposer.Compose(
                    eventType,
                    user.Email,
                    userName,
                    msg,
                    jobId.Value,
                    jobTitle,
                    categoryName ?? "",
                    addressText ?? "",
                    technicianName,
                    technicianPhone);

                await _emailOutbox.EnqueueAsync(
                    composition.ToEmail,
                    composition.Subject,
                    composition.HtmlBody,
                    composition.JobId,
                    notificationId: notifId,
                    ct);
            }
        }

        await _db.Notifications.AddRangeAsync(notifications, ct);
        await _db.SaveChangesAsync(ct);
    }
}
