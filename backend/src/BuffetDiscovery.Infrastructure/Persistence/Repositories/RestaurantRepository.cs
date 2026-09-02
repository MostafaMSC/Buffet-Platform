using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuffetDiscovery.Infrastructure.Persistence.Repositories;

public class RestaurantRepository(AppDbContext db) : IRestaurantRepository
{
    public Task<Restaurant?> GetByIdAsync(int id, CancellationToken ct) =>
        db.Restaurants.Include(r => r.Area).FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<Restaurant?> GetApprovedWithOfferingsAsync(int id, CancellationToken ct) =>
        db.Restaurants
            .Include(r => r.Area)
            .Include(r => r.Offerings.Where(o => !o.IsDeleted))
                .ThenInclude(o => o.Photos)
            .FirstOrDefaultAsync(r => r.Id == id && r.Status == RestaurantStatus.Approved, ct);

    public async Task<List<Restaurant>> GetForAdminAsync(RestaurantStatus? status, CancellationToken ct)
    {
        var query = db.Restaurants.Include(r => r.Area).Include(r => r.Offerings).AsQueryable();
        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }
        return await query.OrderByDescending(r => r.CreatedAt).ToListAsync(ct);
    }

    public void Add(Restaurant restaurant) => db.Restaurants.Add(restaurant);
}
