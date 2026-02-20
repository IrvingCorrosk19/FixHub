namespace FixHub.Domain.Entities;

/// <summary>
/// Incidencia o problema reportado por un cliente sobre un trabajo.
/// </summary>
public class JobIssue
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public Guid ReportedByUserId { get; set; }

    /// <summary>Código de razón: no_contact | late | bad_service | other</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>Descripción adicional opcional.</summary>
    public string? Detail { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Job Job { get; set; } = null!;
    public User ReportedBy { get; set; } = null!;
}
