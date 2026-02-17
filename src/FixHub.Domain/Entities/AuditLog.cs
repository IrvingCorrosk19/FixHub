namespace FixHub.Domain.Entities;

/// <summary>
/// FASE 5.4: Registro de auditoría sin PII (no email, password ni tokens).
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public Guid? ActorUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string? MetadataJson { get; set; }
    public string? CorrelationId { get; set; }
}
