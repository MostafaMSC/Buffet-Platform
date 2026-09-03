namespace BuffetDiscovery.Domain.Entities;

/// The concrete Phase 2 notification channel: an in-dashboard feed for the restaurant.
/// Created via INotificationService, whose customer-facing counterpart currently just
/// logs — this entity plus that port is the extension point a later WhatsApp/SMS
/// implementation plugs into, without touching the booking/cancellation call sites.
public class Notification
{
    public int Id { get; set; }

    public int RestaurantId { get; set; }
    public Restaurant? Restaurant { get; set; }

    public string Message { get; set; } = string.Empty;
    public string? MessageAr { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
