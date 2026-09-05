namespace BuffetDiscovery.Domain.Entities;

public class AvailabilityStatus
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public Service? Service { get; set; }
    public DateOnly Date { get; set; }
    public bool IsActive { get; set; }
}
