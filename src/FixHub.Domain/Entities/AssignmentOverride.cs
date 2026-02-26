namespace FixHub.Domain.Entities;

/// <summary>Reasignación manual de un job a otro técnico por admin/ops. Solo auditoría y trazabilidad.</summary>
public class AssignmentOverride
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public Guid? FromTechnicianId { get; set; }
    public Guid ToTechnicianId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? ReasonDetail { get; set; }
    public Guid AdminUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public Job Job { get; set; } = null!;
}
