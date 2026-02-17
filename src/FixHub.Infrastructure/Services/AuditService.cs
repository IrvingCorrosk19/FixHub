using System.Text.Json;
using FixHub.Application.Common.Interfaces;
using FixHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FixHub.Infrastructure.Services;

/// <summary>
/// FASE 5.4: Implementación de auditoría en DB. No persiste PII (email, password, tokens).
/// </summary>
public class AuditService(
    IApplicationDbContext db,
    ICorrelationIdAccessor correlationIdAccessor) : IAuditService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public async Task LogAsync(
        Guid? actorUserId,
        string action,
        string? entityType,
        Guid? entityId,
        object? metadata,
        CancellationToken cancellationToken = default)
    {
        string? metadataJson = null;
        if (metadata is not null)
        {
            try
            {
                metadataJson = JsonSerializer.Serialize(metadata, JsonOptions);
            }
            catch
            {
                // Si falla la serialización (ej. referencias circulares), no guardar metadata
            }
        }

        var entry = new AuditLog
        {
            Id = Guid.NewGuid(),
            CreatedAtUtc = DateTime.UtcNow,
            ActorUserId = actorUserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            MetadataJson = metadataJson,
            CorrelationId = correlationIdAccessor.GetCorrelationId()
        };

        db.AuditLogs.Add(entry);
        await db.SaveChangesAsync(cancellationToken);
    }
}
