using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuffetDiscovery.Infrastructure.Persistence.Repositories;

public class ServiceRepository(AppDbContext db) : IServiceRepository
{
    public Task<List<Service>> GetByRestaurantAsync(int restaurantId, CancellationToken ct) =>
        db.Services
            .Include(s => s.Photos)
            .Include(s => s.TimeSlots)
            .Include(s => s.MenuSections).ThenInclude(m => m.Items)
            .Where(s => s.RestaurantId == restaurantId && !s.IsDeleted)
            .OrderBy(s => s.ServiceType).ThenBy(s => s.Name)
            .AsSplitQuery()
            .ToListAsync(ct);

    public Task<Service?> GetByIdForRestaurantAsync(int serviceId, int restaurantId, CancellationToken ct) =>
        db.Services
            .Include(s => s.Photos)
            .Include(s => s.TimeSlots)
            .Include(s => s.MenuSections).ThenInclude(m => m.Items)
            .AsSplitQuery()
            .FirstOrDefaultAsync(s => s.Id == serviceId && s.RestaurantId == restaurantId && !s.IsDeleted, ct);

    public Task<Service?> GetByIdAsync(int serviceId, CancellationToken ct) =>
        db.Services.Include(s => s.Photos).FirstOrDefaultAsync(s => s.Id == serviceId, ct);

    public Task<Service?> GetPublicByIdAsync(int serviceId, CancellationToken ct) =>
        db.Services
            .Include(s => s.Photos)
            .Include(s => s.TimeSlots)
            .Include(s => s.MenuSections).ThenInclude(m => m.Items)
            .Include(s => s.Restaurant)!.ThenInclude(r => r!.Area)!.ThenInclude(a => a!.City)!.ThenInclude(c => c!.Country)
            .AsSplitQuery()
            .FirstOrDefaultAsync(s =>
                s.Id == serviceId &&
                !s.IsDeleted &&
                s.Status == ServiceStatus.Active &&
                s.Restaurant!.Status == RestaurantStatus.Approved, ct);

    public Task<List<Review>> GetReviewsAsync(int restaurantId, int? serviceId, int take, CancellationToken ct)
    {
        var query = db.Reviews.Where(r => r.RestaurantId == restaurantId);

        // A service's own reviews lead; the restaurant's other reviews still say something
        // useful about the kitchen, so they follow rather than being hidden.
        return query
            .OrderByDescending(r => r.ServiceId == serviceId)
            .ThenByDescending(r => r.CreatedAt)
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task<HashSet<int>> GetReviewedBookingIdsAsync(IEnumerable<int> bookingIds, CancellationToken ct)
    {
        var ids = bookingIds.ToList();
        if (ids.Count == 0) return [];

        var reviewed = await db.Reviews
            .Where(r => r.BookingId != null && ids.Contains(r.BookingId.Value))
            .Select(r => r.BookingId!.Value)
            .ToListAsync(ct);
        return reviewed.ToHashSet();
    }

    public void Add(Service service) => db.Services.Add(service);

    public void AddReview(Review review) => db.Reviews.Add(review);

    public void RemovePhotos(IEnumerable<ServicePhoto> photos) => db.ServicePhotos.RemoveRange(photos);

    public void RemoveMenuSections(IEnumerable<MenuSection> sections) => db.MenuSections.RemoveRange(sections);
}
