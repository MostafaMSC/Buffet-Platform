namespace BuffetDiscovery.Domain.Entities;

/// 1:1 with Restaurant. Kept as its own table (rather than columns on Restaurant) since
/// these are booking-feature settings introduced in Phase 2, mostly restaurant-editable
/// except where noted.
public class RestaurantSettings
{
    public int Id { get; set; }

    public int RestaurantId { get; set; }
    public Restaurant? Restaurant { get; set; }

    /// How long before a slot/window starts a customer can still cancel their own booking.
    public int CancellationCutoffMinutes { get; set; } = 120;

    /// How many minutes a waitlist offer stays open before lazily passing to the next person.
    public int WaitlistOfferWindowMinutes { get; set; } = 30;

    /// Percent above stated capacity the restaurant is willing to accept bookings for,
    /// anticipating a known no-show rate. 0 = off (default) — restaurant-editable.
    public int OverbookingTolerancePercent { get; set; }

    /// Early-adopter badge. Admin-set only (not restaurant-editable).
    public bool IsFoundingRestaurant { get; set; }

    /// Which restaurant referred this one, if any. Reward (extra featured days etc.) is
    /// handled manually by admin for now, not automated. Admin-set only.
    public int? ReferredByRestaurantId { get; set; }
    public Restaurant? ReferredBy { get; set; }

    /// Simple manually-set ranking boost for search results — higher shows first.
    /// Admin-set only; not auto-computed from booking completion yet (deliberately simple
    /// per Phase 2 scope).
    public int FeaturedScore { get; set; }
}
