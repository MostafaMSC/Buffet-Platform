using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuffetDiscovery.Infrastructure.Persistence.Repositories;

public class OfferingRepository(AppDbContext db) : IOfferingRepository
{
    public async Task<List<BuffetOffering>> GetBrowseCandidatesAsync(int? areaId, MealType? mealType, CancellationToken ct)
    {
        var query = db.Offerings
            .Include(o => o.Restaurant)!.ThenInclude(r => r!.Area)
            .Where(o => !o.IsDeleted && o.Restaurant!.Status == RestaurantStatus.Approved);

        if (areaId.HasValue)
        {
            query = query.Where(o => o.Restaurant!.AreaId == areaId.Value);
        }

        if (mealType.HasValue)
        {
            query = query.Where(o => o.MealType == mealType.Value);
        }

        return await query.ToListAsync(ct);
    }

    public Task<List<BuffetOffering>> GetByRestaurantAsync(int restaurantId, CancellationToken ct) =>
        db.Offerings
            .Include(o => o.Photos)
            .Where(o => o.RestaurantId == restaurantId && !o.IsDeleted)
            .ToListAsync(ct);

    public Task<BuffetOffering?> GetByIdForRestaurantAsync(int offeringId, int restaurantId, CancellationToken ct) =>
        db.Offerings
            .Include(o => o.Photos)
            .FirstOrDefaultAsync(o => o.Id == offeringId && o.RestaurantId == restaurantId && !o.IsDeleted, ct);

    public Task<BuffetOffering?> GetByIdAsync(int offeringId, CancellationToken ct) =>
        db.Offerings.Include(o => o.Photos).FirstOrDefaultAsync(o => o.Id == offeringId, ct);

    public void Add(BuffetOffering offering) => db.Offerings.Add(offering);

    public void RemovePhotos(IEnumerable<OfferingPhoto> photos) => db.OfferingPhotos.RemoveRange(photos);
}
