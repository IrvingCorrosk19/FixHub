namespace FixHub.Domain.Entities;

public class ServiceCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Job> Jobs { get; set; } = [];
}
