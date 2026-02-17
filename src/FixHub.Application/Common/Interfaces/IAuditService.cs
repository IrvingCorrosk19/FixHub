namespace FixHub.Application.Common.Interfaces;

/// <summary>
/// FASE 5.4: Servicio de auditoría. No almacenar PII (email, password, tokens) en metadata.
/// </summary>
public interface IAuditService
{
    Task LogAsync(
        Guid? actorUserId,
        string action,
        string? entityType,
        Guid? entityId,
        object? metadata,
        CancellationToken cancellationToken = default);
}
