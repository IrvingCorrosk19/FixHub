using FixHub.Domain.Enums;

namespace FixHub.Domain.Entities;

/// <summary>
/// FASE 13: Outbox para emails externos.
/// Almacena ToEmail, Subject, HtmlBody directamente para envío asíncrono.
/// </summary>
public class NotificationOutbox
{
    public Guid Id { get; set; }
    /// <summary>FASE 14: Opcional; para índice único (NotificationId, Channel) contra duplicados.</summary>
    public Guid? NotificationId { get; set; }
    public string Channel { get; set; } = "Email";
    public string ToEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public OutboxStatus Status { get; set; } = OutboxStatus.Pending;
    public int Attempts { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>FASE 14: Actualizado cuando cambia Status o Attempts. Permite detectar huérfanos Processing.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    /// <summary>FASE 14: Retry exponencial — siguiente intento permitido. Null = disponible inmediatamente.</summary>
    public DateTime? NextRetryAt { get; set; }
    public Guid? JobId { get; set; }
}
