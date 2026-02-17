using FixHub.Domain.Enums;

namespace FixHub.Domain.Entities;

public class Payment
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? Provider { get; set; }
    public string? ProviderRef { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Job Job { get; set; } = null!;
}
