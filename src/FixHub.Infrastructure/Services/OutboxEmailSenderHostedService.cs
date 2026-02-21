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
/// FASE 14 HARDENING:
///   - Orphan recovery: resetea registros Processing &gt; 5min → Pending.
///   - Retry exponencial: Delay = 10s × 2^(Attempts-1). Ej: 10s, 20s, 40s.
///   - Máximo 4 intentos antes de marcar como Failed.
///   - UPDATE atómico con FOR UPDATE SKIP LOCKED (PostgreSQL).
///   - Timeout por envío: 30 segundos.
/// </summary>
public class OutboxEmailSenderHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxEmailSenderHostedService> _log;
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan SendTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan OrphanThreshold = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan BaseRetryDelay = TimeSpan.FromSeconds(10);
    private const int BatchSize = 20;
    private const int MaxAttempts = 4;

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

        // FASE 14 ORPHAN RECOVERY: resetear registros atascados en Processing.
        // Si un servidor murió mientras enviaba, estos registros quedarían bloqueados.
        await RecoverOrphanedItemsAsync(db, ct);

        List<Domain.Entities.NotificationOutbox> pending;
        await using (var transaction = await db.Database.BeginTransactionAsync(ct))
        {
            // Sólo procesar Pending con NextRetryAt vencido (o sin NextRetryAt).
            pending = await db.NotificationOutbox
                .FromSqlRaw(
                    @"SELECT * FROM notification_outbox
                      WHERE status = 0
                        AND (next_retry_at IS NULL OR next_retry_at <= NOW())
                      ORDER BY created_at
                      LIMIT {0}
                      FOR UPDATE SKIP LOCKED",
                    BatchSize)
                .ToListAsync(ct);

            if (pending.Count == 0) return;

            var now = DateTime.UtcNow;
            foreach (var o in pending)
            {
                o.Status = OutboxStatus.Processing;
                o.UpdatedAt = now;
            }
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
                    item.UpdatedAt = DateTime.UtcNow;
                    _log.LogDebug("Outbox sent. OutboxId={OutboxId} JobId={JobId} ToEmail={ToEmail} Status=Sent", item.Id, item.JobId, item.ToEmail);
                }
                else
                {
                    ApplyRetryOrFail(item, "send returned false");
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                ApplyRetryOrFail(item, "send timeout");
                _log.LogWarning("Outbox send timeout. OutboxId={OutboxId} JobId={JobId} ToEmail={ToEmail} Attempts={Attempts} Status={Status}",
                    item.Id, item.JobId, item.ToEmail, item.Attempts, item.Status);
            }
            catch (Exception ex)
            {
                ApplyRetryOrFail(item, ex.Message);
                _log.LogWarning(ex, "Outbox item failed. OutboxId={OutboxId} JobId={JobId} ToEmail={ToEmail} Attempts={Attempts} Status={Status}",
                    item.Id, item.JobId, item.ToEmail, item.Attempts, item.Status);
            }
        }

        if (pending.Count > 0)
            await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// FASE 14: Resetea registros Processing &gt; OrphanThreshold minutos → Pending.
    /// Protege contra emails atrapados cuando un proceso muere durante el envío.
    /// </summary>
    private async Task RecoverOrphanedItemsAsync(AppDbContext db, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow - OrphanThreshold;
        var orphans = await db.NotificationOutbox
            .Where(o => o.Status == OutboxStatus.Processing && o.UpdatedAt < cutoff)
            .ToListAsync(ct);

        if (orphans.Count == 0) return;

        var now = DateTime.UtcNow;
        foreach (var o in orphans)
        {
            o.Status = OutboxStatus.Pending;
            o.UpdatedAt = now;
            o.NextRetryAt = null;
        }
        await db.SaveChangesAsync(ct);

        _log.LogWarning("Outbox orphan recovery: {Count} registros Processing atascados resetados a Pending.", orphans.Count);
    }

    /// <summary>
    /// FASE 14: Aplica retry exponencial o marca como Failed si se alcanzó MaxAttempts.
    /// Delay = BaseRetryDelay × 2^(Attempts-1): 10s, 20s, 40s.
    /// </summary>
    private void ApplyRetryOrFail(Domain.Entities.NotificationOutbox item, string reason)
    {
        item.Attempts++;
        item.UpdatedAt = DateTime.UtcNow;

        if (item.Attempts >= MaxAttempts)
        {
            item.Status = OutboxStatus.Failed;
            item.NextRetryAt = null;
            _log.LogWarning(
                "Outbox permanently failed after {Attempts} attempts. OutboxId={OutboxId} JobId={JobId} ToEmail={ToEmail} Reason={Reason}",
                item.Attempts, item.Id, item.JobId, item.ToEmail, reason);
        }
        else
        {
            // Retry exponencial: Delay = BaseDelay × 2^(Attempts-1)
            var delaySeconds = BaseRetryDelay.TotalSeconds * Math.Pow(2, item.Attempts - 1);
            item.NextRetryAt = DateTime.UtcNow.AddSeconds(delaySeconds);
            item.Status = OutboxStatus.Pending;
            _log.LogWarning(
                "Outbox retry scheduled. OutboxId={OutboxId} JobId={JobId} ToEmail={ToEmail} Attempts={Attempts} NextRetryAt={NextRetryAt} Reason={Reason}",
                item.Id, item.JobId, item.ToEmail, item.Attempts, item.NextRetryAt, reason);
        }
    }
}
