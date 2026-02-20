using FixHub.Domain.Enums;

namespace FixHub.Application.Common.Interfaces;

/// <summary>
/// FASE 10: Servicio de notificaciones internas (sin email/WhatsApp).
/// </summary>
public interface INotificationService
{
    /// <summary>Envía una notificación a un usuario.</summary>
    Task NotifyAsync(Guid userId, NotificationType type, string message, Guid? jobId = null, CancellationToken ct = default);

    /// <summary>Envía notificaciones a múltiples usuarios.</summary>
    Task NotifyManyAsync(IEnumerable<Guid> userIds, NotificationType type, string message, Guid? jobId = null, CancellationToken ct = default);
}
