using FixHub.Domain.Enums;

namespace FixHub.Domain.Entities;

public class TechnicianProfile
{
    public Guid UserId { get; set; }
    public TechnicianStatus Status { get; set; } = TechnicianStatus.Pending;
    public string? Bio { get; set; }
    public int ServiceRadiusKm { get; set; } = 10;
    public bool IsVerified { get; set; } = false;
    public string? DocumentsJson { get; set; }
    public decimal AvgRating { get; set; } = 0;
    public int CompletedJobs { get; set; } = 0;
    public decimal CancelRate { get; set; } = 0;

    // Navigation
    public User User { get; set; } = null!;

    // ScoreSnapshots usa TechnicianId que apunta al UserId (PK de TechnicianProfile)
    public ICollection<ScoreSnapshot> ScoreSnapshots { get; set; } = [];
}
