using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace BuffetDiscovery.Infrastructure.Persistence.Repositories;

public class BookingRepository(AppDbContext db) : IBookingRepository
{
    /// Bookings that hold a seat: confirmed, awaiting the restaurant's answer, or already
    /// seated. Cancelled, rejected and no-show bookings release their seats.
    private static readonly BookingStatus[] SeatHoldingStatuses =
        [BookingStatus.Confirmed, BookingStatus.Pending, BookingStatus.CheckedIn];

    /// Everything a customer-facing booking view needs to name the restaurant and place.
    private IIncludableQueryable<Booking, City?> WithFullDetail() =>
        db.Bookings
            .Include(b => b.TimeSlot)
            .Include(b => b.Service)!.ThenInclude(s => s!.Photos)
            .Include(b => b.Service)!.ThenInclude(s => s!.Restaurant)!
                .ThenInclude(r => r!.Area)!.ThenInclude(a => a!.City);

    public async Task<int> GetBookedPartySizeAsync(int? timeSlotId, int serviceId, DateOnly date, CancellationToken ct)
    {
        var query = db.Bookings.Where(b =>
            b.ServiceId == serviceId && b.Date == date && SeatHoldingStatuses.Contains(b.Status));

        query = timeSlotId.HasValue
            ? query.Where(b => b.TimeSlotId == timeSlotId)
            : query.Where(b => b.TimeSlotId == null);

        return await query.SumAsync(b => (int?)b.PartySize, ct) ?? 0;
    }

    public Task<Booking?> GetByConfirmationCodeAsync(string code, CancellationToken ct) =>
        WithFullDetail().AsSplitQuery().FirstOrDefaultAsync(b => b.ConfirmationCode == code, ct);

    public Task<bool> ConfirmationCodeExistsAsync(string code, CancellationToken ct) =>
        db.Bookings.AnyAsync(b => b.ConfirmationCode == code, ct);

    public Task<List<Booking>> GetByPhoneAsync(string phone, CancellationToken ct) =>
        WithFullDetail()
            .Where(b => b.CustomerPhone == phone)
            .OrderByDescending(b => b.Date)
            .ThenByDescending(b => b.CreatedAt)
            .AsSplitQuery()
            .ToListAsync(ct);

    public Task<Booking?> GetByIdForRestaurantAsync(int bookingId, int restaurantId, CancellationToken ct) =>
        db.Bookings
            .Include(b => b.TimeSlot)
            .Include(b => b.Service)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.Service!.RestaurantId == restaurantId, ct);

    public async Task<List<Booking>> GetForRestaurantAsync(int restaurantId, DateOnly? date, CancellationToken ct)
    {
        var query = db.Bookings
            .Include(b => b.TimeSlot)
            .Include(b => b.Service)
            .Where(b => b.Service!.RestaurantId == restaurantId);

        if (date.HasValue)
        {
            query = query.Where(b => b.Date == date.Value);
        }

        return await query
            .OrderBy(b => b.Date)
            .ThenBy(b => b.TimeSlot != null ? b.TimeSlot.StartTime : b.Service!.OpensAt)
            .ToListAsync(ct);
    }

    public Task<List<Booking>> GetForAnalyticsAsync(int restaurantId, DateOnly start, DateOnly end, CancellationToken ct) =>
        db.Bookings
            .Include(b => b.TimeSlot)
            .Include(b => b.Service)
            .Where(b => b.Service!.RestaurantId == restaurantId && b.Date >= start && b.Date <= end)
            .ToListAsync(ct);

    public Task<List<Booking>> GetPlatformBookingsAsync(DateOnly start, DateOnly end, CancellationToken ct) =>
        db.Bookings
            .Include(b => b.Service)
            .Where(b => b.Date >= start && b.Date <= end && b.Status != BookingStatus.Cancelled && b.Status != BookingStatus.Rejected)
            .ToListAsync(ct);

    public void Add(Booking booking) => db.Bookings.Add(booking);
}
