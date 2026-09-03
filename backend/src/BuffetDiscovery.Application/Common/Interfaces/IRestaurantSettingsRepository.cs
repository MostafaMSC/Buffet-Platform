using BuffetDiscovery.Domain.Entities;

namespace BuffetDiscovery.Application.Common.Interfaces;

public interface IRestaurantSettingsRepository
{
    Task<RestaurantSettings?> GetByRestaurantIdAsync(int restaurantId, CancellationToken ct);

    /// Ensures a settings row exists (with defaults) for a restaurant that hasn't touched
    /// booking settings yet, so callers never have to null-check.
    Task<RestaurantSettings> GetOrCreateAsync(int restaurantId, CancellationToken ct);

    /// Existing settings rows for a set of restaurants, keyed by RestaurantId. Restaurants
    /// with no row yet (never touched booking settings) are simply absent — callers treat
    /// that as the defaults (IsFoundingRestaurant false, FeaturedScore 0) rather than
    /// materializing a row just to read it, unlike GetOrCreateAsync.
    Task<Dictionary<int, RestaurantSettings>> GetForRestaurantsAsync(IReadOnlyCollection<int> restaurantIds, CancellationToken ct);
}
