namespace FixHub.Domain.Entities;

public class Review
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid TechnicianId { get; set; }

    /// <summary>1 to 5 stars. Enforced by constraint in DB and validator.</summary>
    public int Stars { get; set; }

    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Job Job { get; set; } = null!;
    public User Customer { get; set; } = null!;
    public User Technician { get; set; } = null!;
}
