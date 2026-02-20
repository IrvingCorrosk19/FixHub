namespace FixHub.Domain.Enums;

/// <summary>
/// FASE 10.2: Estado de un registro en NotificationOutbox.
/// </summary>
public enum OutboxStatus
{
    Pending = 0,
    Sent = 1,
    Failed = 2,
    /// <summary>FASE 14: Reclamado por worker; evita que otro worker lo procese.</summary>
    Processing = 3
}
