using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuffetDiscovery.Infrastructure.Persistence.Repositories;

public class BookingRepository(AppDbContext db) : IBookingRepository
{
    public async Task<int> GetBookedPartySizeAsync(int? timeSlotId, int offeringId, DateOnly date, CancellationToken ct)
    {
        var query = db.Bookings.Where(b => b.OfferingId == offeringId && b.Date == date && b.Status == BookingStatus.Confirmed);
        query = timeSlotId.HasValue
            ? query.Where(b => b.TimeSlotId == timeSlotId)
            : query.Where(b => b.TimeSlotId == null);

        return await query.SumAsync(b => (int?)b.PartySize, ct) ?? 0;
    }

    public Task<Booking?> GetByConfirmationCodeAsync(string code, CancellationToken ct) =>
        db.Bookings
            .Include(b => b.Offering)!.ThenInclude(o => o!.Restaurant)
            .Include(b => b.TimeSlot)
            .FirstOrDefaultAsync(b => b.ConfirmationCode == code, ct);

    public Task<bool> ConfirmationCodeExistsAsync(string code, CancellationToken ct) =>
        db.Bookings.AnyAsync(b => b.ConfirmationCode == code, ct);

    public Task<List<Booking>> GetByPhoneAsync(string phone, CancellationToken ct) =>
        db.Bookings
            .Include(b => b.Offering)!.ThenInclude(o => o!.Restaurant)
            .Include(b => b.TimeSlot)
            .Where(b => b.CustomerPhone == phone)
            .OrderByDescending(b => b.Date)
            .ToListAsync(ct);

    public Task<Booking?> GetByIdForRestaurantAsync(int bookingId, int restaurantId, CancellationToken ct) =>
        db.Bookings
            .Include(b => b.TimeSlot)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.Offering!.RestaurantId == restaurantId, ct);

    public async Task<List<Booking>> GetForRestaurantAsync(int restaurantId, DateOnly? date, CancellationToken ct)
    {
        var query = db.Bookings
            .Include(b => b.TimeSlot)
            .Include(b => b.Offering)
            .Where(b => b.Offering!.RestaurantId == restaurantId);

        if (date.HasValue)
        {
            query = query.Where(b => b.Date == date.Value);
        }

        return await query
            .OrderBy(b => b.Date)
            .ThenBy(b => b.TimeSlot != null ? b.TimeSlot.StartTime : b.Offering!.OpensAt)
            .ToListAsync(ct);
    }

    public Task<List<Booking>> GetForAnalyticsAsync(int restaurantId, DateOnly start, DateOnly end, CancellationToken ct) =>
        db.Bookings
            .Include(b => b.TimeSlot)
            .Where(b => b.Offering!.RestaurantId == restaurantId && b.Date >= start && b.Date <= end)
            .ToListAsync(ct);

    public Task<List<Booking>> GetPlatformBookingsAsync(DateOnly start, DateOnly end, CancellationToken ct) =>
        db.Bookings
            .Include(b => b.Offering)
            .Where(b => b.Date >= start && b.Date <= end && b.Status != BookingStatus.Cancelled)
            .ToListAsync(ct);

    public void Add(Booking booking) => db.Bookings.Add(booking);
}
