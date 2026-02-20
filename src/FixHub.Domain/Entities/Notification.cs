using FixHub.Domain.Enums;

namespace FixHub.Domain.Entities;

/// <summary>
/// FASE 10: Notificación interna (sin email/WhatsApp aún).
/// </summary>
public class Notification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? JobId { get; set; }
    public NotificationType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
    public Job? Job { get; set; }
}
