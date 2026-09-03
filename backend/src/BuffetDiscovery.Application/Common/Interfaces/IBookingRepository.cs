using BuffetDiscovery.Domain.Entities;

namespace BuffetDiscovery.Application.Common.Interfaces;

public interface IBookingRepository
{
    /// Sum of PartySize across Confirmed bookings for a slot (or whole service window
    /// when timeSlotId is null) on one date — what capacity is actually checked against.
    Task<int> GetBookedPartySizeAsync(int? timeSlotId, int serviceId, DateOnly date, CancellationToken ct);

    Task<Booking?> GetByConfirmationCodeAsync(string code, CancellationToken ct);
    Task<bool> ConfirmationCodeExistsAsync(string code, CancellationToken ct);
    Task<List<Booking>> GetByPhoneAsync(string phone, CancellationToken ct);
    Task<Booking?> GetByIdForRestaurantAsync(int bookingId, int restaurantId, CancellationToken ct);

    /// All bookings for a restaurant, optionally filtered to one date, newest slot first.
    Task<List<Booking>> GetForRestaurantAsync(int restaurantId, DateOnly? date, CancellationToken ct);

    Task<List<Booking>> GetForAnalyticsAsync(int restaurantId, DateOnly start, DateOnly end, CancellationToken ct);

    /// Non-cancelled bookings across all restaurants in a date range, for the admin's
    /// platform-wide booking volume view.
    Task<List<Booking>> GetPlatformBookingsAsync(DateOnly start, DateOnly end, CancellationToken ct);

    void Add(Booking booking);
}
