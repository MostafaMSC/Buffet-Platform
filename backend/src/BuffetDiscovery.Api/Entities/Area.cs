namespace BuffetDiscovery.Api.Entities;

public class Area
{
    public int Id { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public List<Restaurant> Restaurants { get; set; } = [];
}
