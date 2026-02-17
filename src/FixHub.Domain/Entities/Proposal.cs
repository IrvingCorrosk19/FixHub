using FixHub.Domain.Enums;

namespace FixHub.Domain.Entities;

public class Proposal
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public Guid TechnicianId { get; set; }
    public decimal Price { get; set; }
    public string? Message { get; set; }
    public ProposalStatus Status { get; set; } = ProposalStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Job Job { get; set; } = null!;
    public User Technician { get; set; } = null!;
    public JobAssignment? Assignment { get; set; }
}
