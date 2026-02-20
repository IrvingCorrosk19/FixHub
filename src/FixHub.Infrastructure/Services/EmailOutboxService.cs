using FixHub.Application.Common.Interfaces;
using FixHub.Domain.Entities;
using FixHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace FixHub.Infrastructure.Services;

/// <summary>
/// FASE 13/14: Encola emails en NotificationOutbox sin bloquear comandos.
/// FASE 14: Índice único (NotificationId, Channel); maneja duplicados con warning.
/// </summary>
public class EmailOutboxService : IEmailOutboxService
{
    private readonly IApplicationDbContext _db;
    private readonly ILogger<EmailOutboxService> _log;

    public EmailOutboxService(IApplicationDbContext db, ILogger<EmailOutboxService> log)
    {
        _db = db;
        _log = log;
    }

    public async Task EnqueueAsync(string toEmail, string subject, string htmlBody, Guid? jobId = null, Guid? notificationId = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(toEmail)) return;

        _db.NotificationOutbox.Add(new NotificationOutbox
        {
            Id = Guid.NewGuid(),
            NotificationId = notificationId,
            Channel = "Email",
            ToEmail = toEmail.Trim(),
            Subject = subject,
            HtmlBody = htmlBody,
            Status = OutboxStatus.Pending,
            Attempts = 0,
            CreatedAt = DateTime.UtcNow,
            JobId = jobId
        });

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            _log.LogWarning(ex, "Duplicate outbox entry rejected. NotificationId={NotificationId} Channel=Email ToEmail={ToEmail}",
                notificationId, toEmail);
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException?.Message?.Contains("unique", StringComparison.OrdinalIgnoreCase) == true
        || ex.InnerException?.Message?.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true
        || (ex.InnerException as PostgresException)?.SqlState == "23505";
}
