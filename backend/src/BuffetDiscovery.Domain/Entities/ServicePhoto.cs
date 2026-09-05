namespace BuffetDiscovery.Domain.Entities;

public class ServicePhoto
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public Service? Service { get; set; }
    public string Url { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
