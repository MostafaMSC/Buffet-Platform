using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuffetDiscovery.Infrastructure.Persistence.Repositories;

public class SearchRepository(AppDbContext db) : ISearchRepository
{
    public async Task<List<Service>> FindCandidatesAsync(ServiceSearchFilter filter, CancellationToken ct)
    {
        var query = db.Services
            .Include(s => s.Photos)
            .Include(s => s.TimeSlots)
            .Include(s => s.Restaurant)!.ThenInclude(r => r!.Area)!.ThenInclude(a => a!.City)!.ThenInclude(c => c!.Country)
            .Where(s => !s.IsDeleted
                && s.Status == ServiceStatus.Active
                && s.Restaurant!.Status == RestaurantStatus.Approved);

        if (filter.ServiceType.HasValue)
        {
            query = query.Where(s => s.ServiceType == filter.ServiceType.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.CitySlug))
        {
            query = query.Where(s => s.Restaurant!.Area!.City!.Slug == filter.CitySlug);
        }

        if (filter.AreaId.HasValue)
        {
            query = query.Where(s => s.Restaurant!.AreaId == filter.AreaId.Value);
        }

        // Price compares against whichever number the customer actually pays: the package
        // price for package services, the adult price otherwise.
        if (filter.MinPrice.HasValue)
        {
            query = query.Where(s => (s.PricingModel == PricingModel.PerPackage ? s.PackagePrice ?? 0 : s.PricePerAdult) >= filter.MinPrice.Value);
        }

        if (filter.MaxPrice.HasValue)
        {
            query = query.Where(s => (s.PricingModel == PricingModel.PerPackage ? s.PackagePrice ?? 0 : s.PricePerAdult) <= filter.MaxPrice.Value);
        }

        // Flags filters are "matches any of the selected" — a bitwise AND that Npgsql
        // translates, so these stay in SQL rather than pulling rows into memory.
        if (filter.Cuisines != Cuisines.None)
        {
            query = query.Where(s => (s.Cuisines & filter.Cuisines) != Cuisines.None);
        }

        // Dietary is "must have all selected", since a vegan guest needs every box ticked.
        if (filter.Dietary != DietaryTags.None)
        {
            query = query.Where(s => (s.Dietary & filter.Dietary) == filter.Dietary);
        }

        if (filter.Features != RestaurantFeatures.None)
        {
            query = query.Where(s => (s.Restaurant!.Features & filter.Features) == filter.Features);
        }

        if (filter.MealTypes is { Length: > 0 })
        {
            query = query.Where(s => filter.MealTypes.Contains(s.MealType));
        }

        if (filter.BookingMode.HasValue)
        {
            query = query.Where(s => s.BookingMode == filter.BookingMode.Value);
        }

        if (filter.Guests.HasValue)
        {
            var guests = filter.Guests.Value;
            query = query.Where(s => s.MinGuests <= guests && (s.MaxGuests == null || s.MaxGuests >= guests));
        }

        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            var term = $"%{filter.Query.Trim()}%";
            query = query.Where(s =>
                EF.Functions.ILike(s.Name, term) ||
                EF.Functions.ILike(s.NameAr, term) ||
                EF.Functions.ILike(s.Restaurant!.Name, term) ||
                EF.Functions.ILike(s.Restaurant!.NameAr, term));
        }

        return await query.AsSplitQuery().ToListAsync(ct);
    }

    public async Task<Dictionary<(int ServiceId, int? TimeSlotId), int>> GetBookedGuestsAsync(
        IReadOnlyCollection<int> serviceIds, DateOnly date, CancellationToken ct)
    {
        var rows = await db.Bookings
            .Where(b => serviceIds.Contains(b.ServiceId)
                && b.Date == date
                && (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Pending || b.Status == BookingStatus.CheckedIn))
            .GroupBy(b => new { b.ServiceId, b.TimeSlotId })
            .Select(g => new { g.Key.ServiceId, g.Key.TimeSlotId, Guests = g.Sum(b => b.PartySize) })
            .ToListAsync(ct);

        return rows.ToDictionary(r => (r.ServiceId, r.TimeSlotId), r => r.Guests);
    }

    public async Task<Dictionary<(int TimeSlotId, DateOnly Date), SlotOverride>> GetSlotOverridesAsync(
        IReadOnlyCollection<int> serviceIds, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var rows = await db.SlotOverrides
            .Where(o => serviceIds.Contains(o.TimeSlot!.ServiceId) && o.Date >= from && o.Date <= to)
            .ToListAsync(ct);

        return rows.ToDictionary(o => (o.TimeSlotId, o.Date));
    }

    public async Task<Dictionary<(int ServiceId, DateOnly Date), bool>> GetDayStatusesAsync(
        IReadOnlyCollection<int> serviceIds, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var rows = await db.AvailabilityStatuses
            .Where(a => serviceIds.Contains(a.ServiceId) && a.Date >= from && a.Date <= to)
            .Select(a => new { a.ServiceId, a.Date, a.IsActive })
            .ToListAsync(ct);

        return rows.ToDictionary(a => (a.ServiceId, a.Date), a => a.IsActive);
    }

    public async Task<Dictionary<int, (double Average, int Count)>> GetRatingsAsync(
        IReadOnlyCollection<int> restaurantIds, CancellationToken ct)
    {
        var rows = await db.Reviews
            .Where(r => restaurantIds.Contains(r.RestaurantId))
            .GroupBy(r => r.RestaurantId)
            .Select(g => new { RestaurantId = g.Key, Average = g.Average(r => (double)r.Rating), Count = g.Count() })
            .ToListAsync(ct);

        return rows.ToDictionary(r => r.RestaurantId, r => (r.Average, r.Count));
    }

    public async Task<Dictionary<int, int>> GetRecentBookingCountsAsync(
        IReadOnlyCollection<int> serviceIds, DateOnly since, CancellationToken ct)
    {
        var rows = await db.Bookings
            .Where(b => serviceIds.Contains(b.ServiceId) && b.Date >= since && b.Status != BookingStatus.Cancelled && b.Status != BookingStatus.Rejected)
            .GroupBy(b => b.ServiceId)
            .Select(g => new { ServiceId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return rows.ToDictionary(r => r.ServiceId, r => r.Count);
    }

    public async Task<Dictionary<int, RestaurantSettings>> GetSettingsAsync(
        IReadOnlyCollection<int> restaurantIds, CancellationToken ct)
    {
        var rows = await db.RestaurantSettings
            .Where(s => restaurantIds.Contains(s.RestaurantId))
            .ToListAsync(ct);

        return rows.ToDictionary(s => s.RestaurantId);
    }

    public async Task<List<(City City, int ServiceCount)>> GetCitiesWithCountsAsync(CancellationToken ct)
    {
        var rows = await db.Cities
            .OrderBy(c => c.SortOrder)
            .Select(c => new
            {
                City = c,
                ServiceCount = db.Services.Count(s =>
                    !s.IsDeleted &&
                    s.Status == ServiceStatus.Active &&
                    s.Restaurant!.Status == RestaurantStatus.Approved &&
                    s.Restaurant!.Area!.CityId == c.Id)
            })
            .ToListAsync(ct);

        return rows.Select(r => (r.City, r.ServiceCount)).ToList();
    }

    public Task<List<Country>> GetLocationTreeAsync(CancellationToken ct) =>
        db.Countries
            .Include(c => c.Cities.OrderBy(city => city.SortOrder))
            .ThenInclude(c => c.Areas.OrderBy(a => a.SortOrder))
            .OrderBy(c => c.SortOrder)
            .ToListAsync(ct);
}
