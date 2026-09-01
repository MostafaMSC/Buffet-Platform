namespace BuffetDiscovery.Api.Entities;

public class AvailabilityStatus
{
    public int Id { get; set; }
    public int OfferingId { get; set; }
    public BuffetOffering? Offering { get; set; }
    public DateOnly Date { get; set; }
    public bool IsActive { get; set; }
}
