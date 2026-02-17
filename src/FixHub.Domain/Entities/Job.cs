using FixHub.Domain.Enums;

namespace FixHub.Domain.Entities;

public class Job
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public int CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AddressText { get; set; } = string.Empty;
    public decimal? Lat { get; set; }
    public decimal? Lng { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Open;
    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User Customer { get; set; } = null!;
    public ServiceCategory Category { get; set; } = null!;
    public ICollection<Proposal> Proposals { get; set; } = [];
    public JobAssignment? Assignment { get; set; }
    public Review? Review { get; set; }
    public Payment? Payment { get; set; }
    public ICollection<ScoreSnapshot> ScoreSnapshots { get; set; } = [];
}
