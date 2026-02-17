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

    // Navigation
    public TechnicianProfile? TechnicianProfile { get; set; }
    public ICollection<Job> JobsAsCustomer { get; set; } = [];
    public ICollection<Proposal> Proposals { get; set; } = [];
    public ICollection<Review> ReviewsGiven { get; set; } = [];
    public ICollection<Review> ReviewsReceived { get; set; } = [];
}
