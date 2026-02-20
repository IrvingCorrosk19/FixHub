using FixHub.Application.Common.Interfaces;
using FixHub.Domain.Enums;
using FixHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FixHub.Infrastructure.Services;

/// <summary>
/// FASE 13/14: Procesa NotificationOutbox cada 10s, envía con IEmailSender.
/// FASE 14: UPDATE atómico con FOR UPDATE SKIP LOCKED, timeout por envío, logging OutboxId.
/// </summary>
public class OutboxEmailSenderHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxEmailSenderHostedService> _log;
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan SendTimeout = TimeSpan.FromSeconds(30);
    private const int BatchSize = 20;

    public OutboxEmailSenderHostedService(IServiceScopeFactory scopeFactory, ILogger<OutboxEmailSenderHostedService> log)
    {
        _scopeFactory = scopeFactory;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.LogInformation("OutboxEmailSenderHostedService started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "OutboxEmailSender batch error");
            }
            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

        List<Domain.Entities.NotificationOutbox> pending;
        await using (var transaction = await db.Database.BeginTransactionAsync(ct))
        {
            pending = await db.NotificationOutbox
                .FromSqlRaw(
                    @"SELECT * FROM notification_outbox WHERE status = 0 ORDER BY created_at LIMIT {0} FOR UPDATE SKIP LOCKED",
                    BatchSize)
                .ToListAsync(ct);

            if (pending.Count == 0) return;

            foreach (var o in pending) o.Status = OutboxStatus.Processing;
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }

        foreach (var item in pending)
        {
            try
            {
                using var sendCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                sendCts.CancelAfter(SendTimeout);

                var ok = await emailSender.SendEmailAsync(item.ToEmail, item.Subject, item.HtmlBody, sendCts.Token);

                if (ok)
                {
                    item.Status = OutboxStatus.Sent;
                    item.SentAt = DateTime.UtcNow;
                    _log.LogDebug("Outbox sent. OutboxId={OutboxId} JobId={JobId} ToEmail={ToEmail} Status=Sent", item.Id, item.JobId, item.ToEmail);
                }
                else
                {
                    item.Attempts++;
                    if (item.Attempts >= 3) item.Status = OutboxStatus.Failed;
                    _log.LogWarning("Outbox send failed. OutboxId={OutboxId} JobId={JobId} ToEmail={ToEmail} Attempts={Attempts} Status={Status}",
                        item.Id, item.JobId, item.ToEmail, item.Attempts, item.Status);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                item.Attempts++;
                if (item.Attempts >= 3) item.Status = OutboxStatus.Failed;
                _log.LogWarning("Outbox send timeout. OutboxId={OutboxId} JobId={JobId} ToEmail={ToEmail} Attempts={Attempts}", item.Id, item.JobId, item.ToEmail, item.Attempts);
            }
            catch (Exception ex)
            {
                item.Attempts++;
                if (item.Attempts >= 3) item.Status = OutboxStatus.Failed;
                _log.LogWarning(ex, "Outbox item failed. OutboxId={OutboxId} JobId={JobId} ToEmail={ToEmail}", item.Id, item.JobId, item.ToEmail);
            }
        }

        if (pending.Count > 0)
            await db.SaveChangesAsync(ct);
    }
}
