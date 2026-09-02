namespace BuffetDiscovery.Domain.Entities;

public class OfferingPhoto
{
    public int Id { get; set; }
    public int OfferingId { get; set; }
    public BuffetOffering? Offering { get; set; }
    public string Url { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
