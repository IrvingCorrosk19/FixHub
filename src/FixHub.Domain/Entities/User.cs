using FixHub.Domain.Enums;

namespace FixHub.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    /// <summary>Suspensión temporal por admin (impago, fraude, etc.).</summary>
    public bool IsSuspended { get; set; }

    /// <summary>Fin de suspensión (null = indefinida hasta reactivación).</summary>
    public DateTime? SuspendedUntil { get; set; }

    /// <summary>Motivo interno de la suspensión.</summary>
    public string? SuspensionReason { get; set; }

    /// <summary>Baja definitiva de la cuenta.</summary>
    public DateTime? DeactivatedAt { get; set; }

    /// <summary>Token de concurrencia optimista.</summary>
    public byte[]? RowVersion { get; set; }

    // Navigation
    public TechnicianProfile? TechnicianProfile { get; set; }
    public ICollection<Job> JobsAsCustomer { get; set; } = [];
    public ICollection<Proposal> Proposals { get; set; } = [];
    public ICollection<Review> ReviewsGiven { get; set; } = [];
    public ICollection<Review> ReviewsReceived { get; set; } = [];
}
