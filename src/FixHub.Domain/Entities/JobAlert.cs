using FixHub.Domain.Enums;

namespace FixHub.Domain.Entities;

/// <summary>
/// FASE 11: Alerta persistente generada por el motor SLA.
/// </summary>
public class JobAlert
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public JobAlertType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsResolved { get; set; } = false;

    /// <summary>FASE 14: Cuándo fue resuelta la alerta.</summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>FASE 14: Quién resolvió la alerta (admin).</summary>
    public Guid? ResolvedByUserId { get; set; }

    // Navigation
    public Job Job { get; set; } = null!;
}
