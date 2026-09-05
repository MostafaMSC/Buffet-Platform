namespace BuffetDiscovery.Domain.Entities;

public class City
{
    public int Id { get; set; }

    public int CountryId { get; set; }
    public Country? Country { get; set; }

    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;

    /// URL-friendly identifier used in search links, e.g. "baghdad".
    public string Slug { get; set; } = string.Empty;

    /// City centre, used to anchor map view and distance sorting.
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    /// Optional hero image for the "discover by region" section.
    public string? ImageUrl { get; set; }

    public int SortOrder { get; set; }

    public List<Area> Areas { get; set; } = [];
}
