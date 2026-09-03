using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Domain.Entities;

namespace BuffetDiscovery.Application.Common;

/// One place that turns a Booking into the shape every customer-facing surface shows —
/// the confirmation page, the badge, and the My Bookings list all read the same fields, so
/// they can't describe the same booking differently.
public static class BookingMapper
{
    /// Requires the booking's Service → Restaurant → Area → City chain to be loaded.
    public static BookingDetailDto ToDetail(Booking booking, int restaurantCancellationCutoffMinutes)
    {
        var service = booking.Service!;
        var restaurant = service.Restaurant!;
        var area = restaurant.Area;
        var city = area?.City;

        return new BookingDetailDto(
            booking.Id,
            booking.ConfirmationCode,
            restaurant.Id,
            restaurant.Name,
            restaurant.NameAr,
            restaurant.PhoneNumber,
            area?.NameEn ?? string.Empty,
            area?.NameAr ?? string.Empty,
            city?.NameEn ?? string.Empty,
            city?.NameAr ?? string.Empty,
            service.Id,
            service.ServiceType,
            service.Name,
            service.NameAr,
            service.MealType,
            service.Photos.OrderBy(p => p.SortOrder).Select(p => p.Url).FirstOrDefault() ?? restaurant.CoverPhotoUrl,
            booking.Date,
            booking.TimeSlot?.StartTime.ToString("HH:mm"),
            booking.TimeSlot?.EndTime.ToString("HH:mm"),
            booking.CustomerName,
            booking.CustomerPhone,
            booking.CustomerEmail,
            booking.SpecialRequests,
            booking.PartySize,
            booking.Adults,
            booking.Children,
            booking.TotalPrice,
            city?.Country?.CurrencyCode ?? "IQD",
            booking.Status,
            CancellationCutoff(service, restaurantCancellationCutoffMinutes),
            booking.CreatedAt);
    }

    /// A service can set its own cancellation cutoff; otherwise the restaurant-wide setting
    /// applies.
    public static int CancellationCutoff(Service service, int restaurantCutoffMinutes) =>
        service.CancellationCutoffMinutes ?? restaurantCutoffMinutes;
}
