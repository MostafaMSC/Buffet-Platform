using BuffetDiscovery.Application.Common;
using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Common.Interfaces;
using MediatR;

namespace BuffetDiscovery.Application.Features.Booking.Customer;

/// Phone-number lookup: every booking and waitlist entry a customer has made, across all
/// restaurants, without requiring an account.
public record GetMyBookingsQuery(string Phone) : IRequest<MyLookupResultDto>;

public class GetMyBookingsQueryHandler(
    IBookingRepository bookingRepo,
    IServiceRepository serviceRepo,
    IWaitlistRepository waitlistRepo,
    IRestaurantSettingsRepository settingsRepo) : IRequestHandler<GetMyBookingsQuery, MyLookupResultDto>
{
    public async Task<MyLookupResultDto> Handle(GetMyBookingsQuery request, CancellationToken ct)
    {
        var phone = request.Phone.Trim();
        var bookings = await bookingRepo.GetByPhoneAsync(phone, ct);
        var waitlistEntries = await waitlistRepo.GetByPhoneAsync(phone, ct);
        var reviewedIds = await serviceRepo.GetReviewedBookingIdsAsync(bookings.Select(b => b.Id), ct);

        var settingsCache = new Dictionary<int, Domain.Entities.RestaurantSettings>();
        async Task<Domain.Entities.RestaurantSettings> SettingsFor(int restaurantId)
        {
            if (!settingsCache.TryGetValue(restaurantId, out var settings))
            {
                settings = await settingsRepo.GetOrCreateAsync(restaurantId, ct);
                settingsCache[restaurantId] = settings;
            }
            return settings;
        }

        var bookingDtos = new List<BookingDetailDto>();
        foreach (var b in bookings)
        {
            var settings = await SettingsFor(b.Service!.RestaurantId);
            bookingDtos.Add(BookingMapper.ToDetail(b, settings.CancellationCutoffMinutes, reviewedIds.Contains(b.Id)));
        }

        var waitlistDtos = new List<WaitlistDetailDto>();
        foreach (var w in waitlistEntries)
        {
            var service = w.Service!;
            var restaurant = service.Restaurant!;
            var settings = await SettingsFor(restaurant.Id);

            waitlistDtos.Add(new WaitlistDetailDto(
                w.Id, restaurant.Id, restaurant.Name, restaurant.NameAr,
                service.Id, service.MealType, w.Date,
                w.TimeSlot?.StartTime.ToString("HH:mm"), w.TimeSlot?.EndTime.ToString("HH:mm"),
                w.CustomerName, w.CustomerPhone, w.PartySize, w.Position, w.Status, w.NotifiedAt,
                settings.WaitlistOfferWindowMinutes));
        }

        return new MyLookupResultDto(bookingDtos, waitlistDtos);
    }
}
