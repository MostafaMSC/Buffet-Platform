namespace BuffetDiscovery.Domain.Entities;

/// A neighbourhood within a city — the finest-grained location a restaurant sits in.
public class Area
{
    public int Id { get; set; }

    public int CityId { get; set; }
    public City? City { get; set; }

    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public List<Restaurant> Restaurants { get; set; } = [];
}
