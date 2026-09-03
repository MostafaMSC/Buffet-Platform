using BuffetDiscovery.Domain.Entities;

namespace BuffetDiscovery.Application.Common.Interfaces;

public interface IServiceRepository
{
    /// Non-deleted services for one restaurant, with photos, slots and menu loaded — the
    /// restaurant's own list, so paused and draft services are included.
    Task<List<Service>> GetByRestaurantAsync(int restaurantId, CancellationToken ct);

    /// A single service owned by the given restaurant, with photos, slots and menu.
    Task<Service?> GetByIdForRestaurantAsync(int serviceId, int restaurantId, CancellationToken ct);

    /// A single service by id regardless of owner or status (admin use).
    Task<Service?> GetByIdAsync(int serviceId, CancellationToken ct);

    /// A live service of an approved restaurant, with everything the public detail page
    /// needs. Used by booking too — neither should work against a deleted service, a paused
    /// one, or a restaurant that has been suspended since the page was loaded.
    Task<Service?> GetPublicByIdAsync(int serviceId, CancellationToken ct);

    /// Recent reviews for a service's restaurant, newest first.
    Task<List<Review>> GetReviewsAsync(int restaurantId, int? serviceId, int take, CancellationToken ct);

    void Add(Service service);
    void RemovePhotos(IEnumerable<ServicePhoto> photos);
    void RemoveMenuSections(IEnumerable<MenuSection> sections);
}
