namespace FixHub.Domain.Entities;

public class ScoreSnapshot
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public Guid TechnicianId { get; set; }
    public decimal Score { get; set; }

    /// <summary>JSON con factores que generaron el score (para auditoría y explicabilidad).</summary>
    public string FactorsJson { get; set; } = "{}";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Job Job { get; set; } = null!;
    public TechnicianProfile Technician { get; set; } = null!;
}
