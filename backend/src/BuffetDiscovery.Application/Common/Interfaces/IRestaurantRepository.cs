using BuffetDiscovery.Domain.Entities;

namespace BuffetDiscovery.Application.Common.Interfaces;

public interface IRestaurantRepository
{
    /// Includes Area. No status filter.
    Task<Restaurant?> GetByIdAsync(int id, CancellationToken ct);

    /// Includes Area and non-deleted Services with their Photos. Only Approved restaurants.
    Task<Restaurant?> GetApprovedWithServicesAsync(int id, CancellationToken ct);

    Task<List<Restaurant>> GetForAdminAsync(RestaurantStatus? status, CancellationToken ct);

    void Add(Restaurant restaurant);
}
