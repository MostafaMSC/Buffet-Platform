using BuffetDiscovery.Domain.Entities;

namespace BuffetDiscovery.Application.Common.Interfaces;

public interface IOfferingRepository
{
    /// Non-deleted offerings belonging to an Approved restaurant, optionally filtered by
    /// area/meal type. Includes Restaurant and Restaurant.Area. Used for the public browse page.
    Task<List<BuffetOffering>> GetBrowseCandidatesAsync(int? areaId, MealType? mealType, CancellationToken ct);

    /// Non-deleted offerings for one restaurant, with Photos included.
    Task<List<BuffetOffering>> GetByRestaurantAsync(int restaurantId, CancellationToken ct);

    /// A single non-deleted offering owned by the given restaurant, with Photos included.
    Task<BuffetOffering?> GetByIdForRestaurantAsync(int offeringId, int restaurantId, CancellationToken ct);

    /// A single offering by id regardless of owner (admin use), with Photos included.
    Task<BuffetOffering?> GetByIdAsync(int offeringId, CancellationToken ct);

    void Add(BuffetOffering offering);
    void RemovePhotos(IEnumerable<OfferingPhoto> photos);
}
