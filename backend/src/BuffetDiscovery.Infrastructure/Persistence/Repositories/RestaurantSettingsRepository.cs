using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuffetDiscovery.Infrastructure.Persistence.Repositories;

public class RestaurantSettingsRepository(AppDbContext db) : IRestaurantSettingsRepository
{
    public Task<RestaurantSettings?> GetByRestaurantIdAsync(int restaurantId, CancellationToken ct) =>
        db.RestaurantSettings.FirstOrDefaultAsync(s => s.RestaurantId == restaurantId, ct);

    /// Saves immediately when creating a default row, unlike the rest of this codebase's
    /// repositories, so callers get a usable (persisted, Id-assigned) settings row back
    /// without needing to know whether one already existed.
    public async Task<RestaurantSettings> GetOrCreateAsync(int restaurantId, CancellationToken ct)
    {
        var existing = await GetByRestaurantIdAsync(restaurantId, ct);
        if (existing is not null) return existing;

        var created = new RestaurantSettings { RestaurantId = restaurantId };
        db.RestaurantSettings.Add(created);
        await db.SaveChangesAsync(ct);
        return created;
    }

    public Task<Dictionary<int, RestaurantSettings>> GetForRestaurantsAsync(IReadOnlyCollection<int> restaurantIds, CancellationToken ct) =>
        db.RestaurantSettings
            .Where(s => restaurantIds.Contains(s.RestaurantId))
            .ToDictionaryAsync(s => s.RestaurantId, ct);
}
