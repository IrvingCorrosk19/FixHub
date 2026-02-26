namespace FixHub.Domain.Entities;

/// <summary>Historial de cambios de estado de usuario (activo, suspendido). Auditable.</summary>
public class UserStatusHistory
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public bool PreviousIsActive { get; set; }
    public bool PreviousIsSuspended { get; set; }
    public bool NewIsActive { get; set; }
    public bool NewIsSuspended { get; set; }
    public string? Reason { get; set; }
    public Guid ActorUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
    public User ActorUser { get; set; } = null!;
}
