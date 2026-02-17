namespace FixHub.Domain.Entities;

public class JobAssignment
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public Guid ProposalId { get; set; }
    public DateTime AcceptedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Navigation
    public Job Job { get; set; } = null!;
    public Proposal Proposal { get; set; } = null!;
}
