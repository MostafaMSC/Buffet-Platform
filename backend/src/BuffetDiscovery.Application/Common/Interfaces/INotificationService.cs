namespace BuffetDiscovery.Application.Common.Interfaces;

/// The extension point for push-to-WhatsApp/SMS later. Booking/cancellation/waitlist
/// handlers call this and never touch a delivery channel directly, so swapping or adding
/// an implementation (e.g. WhatsApp Business API) doesn't require touching call sites.
public interface INotificationService
{
    /// Restaurant-facing notifications persist to an in-dashboard feed (the concrete
    /// Phase 2 channel — restaurant owners are unlikely to check constantly, but this is
    /// what ships now; WhatsApp/SMS plug in here later).
    Task NotifyRestaurantAsync(int restaurantId, string message, string? messageAr, CancellationToken ct);

    /// Customer-facing notifications have no delivery channel yet in Phase 2 (no accounts,
    /// no SMS/WhatsApp wired up) — the customer instead sees current status by looking up
    /// their booking (badge page / phone lookup). This call exists so the call sites are
    /// already in place for when a real channel is wired up.
    Task NotifyCustomerAsync(string phone, string message, CancellationToken ct);
}
