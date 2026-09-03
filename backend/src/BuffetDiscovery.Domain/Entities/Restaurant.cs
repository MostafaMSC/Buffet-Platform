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

    public string? Description { get; set; }
    public string? DescriptionAr { get; set; }

    public string? LogoUrl { get; set; }
    public string? CoverPhotoUrl { get; set; }

    public RestaurantStatus Status { get; set; } = RestaurantStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<BuffetOffering> Offerings { get; set; } = [];
    public List<User> Users { get; set; } = [];
    public RestaurantSettings? Settings { get; set; }
}
