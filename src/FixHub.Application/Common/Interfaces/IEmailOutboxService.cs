namespace FixHub.Application.Common.Interfaces;

/// <summary>
/// FASE 13: Servicio para encolar emails en el Outbox (sin bloquear comandos).
/// </summary>
public interface IEmailOutboxService
{
    /// <param name="notificationId">FASE 14: opcional; evita duplicados con índice único.</param>
    Task EnqueueAsync(string toEmail, string subject, string htmlBody, Guid? jobId = null, Guid? notificationId = null, CancellationToken ct = default);
}
