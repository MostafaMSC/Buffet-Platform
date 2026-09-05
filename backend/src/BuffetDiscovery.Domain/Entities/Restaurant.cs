namespace BuffetDiscovery.Domain.Entities;

public class Restaurant
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;

    public int AreaId { get; set; }
    public Area? Area { get; set; }

    public string PhoneNumber { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? GoogleMapsUrl { get; set; }

    /// Coordinates power map view and distance sorting. Optional — a restaurant that
    /// hasn't set them is simply excluded from distance-based results.
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public string? Description { get; set; }
    public string? DescriptionAr { get; set; }

    public string? LogoUrl { get; set; }
    public string? CoverPhotoUrl { get; set; }

    /// Facilities shown on the detail page and filterable in search.
    public RestaurantFeatures Features { get; set; } = RestaurantFeatures.None;

    public RestaurantStatus Status { get; set; } = RestaurantStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Service> Services { get; set; } = [];
    public List<User> Users { get; set; } = [];
    public List<Review> Reviews { get; set; } = [];
    public RestaurantSettings? Settings { get; set; }
}
