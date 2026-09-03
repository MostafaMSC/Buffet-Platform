namespace BuffetDiscovery.Domain.Entities;

/// A guest rating. Ratings shown anywhere in the product are averaged from these rows —
/// a restaurant with no reviews shows no rating rather than a placeholder score.
public class Review
{
    public int Id { get; set; }

    public int RestaurantId { get; set; }
    public Restaurant? Restaurant { get; set; }

    /// The specific service reviewed, when the guest reviewed one.
    public int? ServiceId { get; set; }
    public Service? Service { get; set; }

    /// Set when the review came from a completed booking, which is what makes it verified.
    public int? BookingId { get; set; }
    public Booking? Booking { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    /// 1–5.
    public int Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
