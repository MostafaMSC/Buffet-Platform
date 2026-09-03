using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Common.Interfaces;
using MediatR;

namespace BuffetDiscovery.Application.Features.Booking.Customer;

/// Phone-number lookup: lists every booking and waitlist entry a customer has made, across
/// all restaurants, without requiring an account (see Phase 2 clarifying answer #1).
public record GetMyBookingsQuery(string Phone) : IRequest<MyLookupResultDto>;

public class GetMyBookingsQueryHandler(
    IBookingRepository bookingRepo,
    IWaitlistRepository waitlistRepo,
    IRestaurantSettingsRepository settingsRepo) : IRequestHandler<GetMyBookingsQuery, MyLookupResultDto>
{
    public async Task<MyLookupResultDto> Handle(GetMyBookingsQuery request, CancellationToken ct)
    {
        var bookings = await bookingRepo.GetByPhoneAsync(request.Phone, ct);
        var waitlistEntries = await waitlistRepo.GetByPhoneAsync(request.Phone, ct);

        var offerWindowByRestaurant = new Dictionary<int, int>();
        async Task<int> GetOfferWindowAsync(int restaurantId)
        {
            if (!offerWindowByRestaurant.TryGetValue(restaurantId, out var minutes))
            {
                minutes = (await settingsRepo.GetOrCreateAsync(restaurantId, ct)).WaitlistOfferWindowMinutes;
                offerWindowByRestaurant[restaurantId] = minutes;
            }
            return minutes;
        }

        var bookingDtos = bookings.Select(b =>
        {
            var offering = b.Offering!;
            var restaurant = offering.Restaurant!;
            return new BookingDetailDto(
                b.Id, b.ConfirmationCode, restaurant.Id, restaurant.Name, restaurant.NameAr,
                offering.Id, offering.MealType, b.Date,
                b.TimeSlot?.StartTime.ToString("HH:mm"), b.TimeSlot?.EndTime.ToString("HH:mm"),
                b.CustomerName, b.CustomerPhone, b.PartySize, b.Status, b.CreatedAt
            );
        }).ToList();

        var waitlistDtos = new List<WaitlistDetailDto>();
        foreach (var w in waitlistEntries)
        {
            var offering = w.Offering!;
            var restaurant = offering.Restaurant!;
            waitlistDtos.Add(new WaitlistDetailDto(
                w.Id, restaurant.Id, restaurant.Name, restaurant.NameAr,
                offering.Id, offering.MealType, w.Date,
                w.TimeSlot?.StartTime.ToString("HH:mm"), w.TimeSlot?.EndTime.ToString("HH:mm"),
                w.CustomerName, w.CustomerPhone, w.PartySize, w.Position, w.Status, w.NotifiedAt,
                await GetOfferWindowAsync(restaurant.Id)
            ));
        }

        return new MyLookupResultDto(bookingDtos, waitlistDtos);
    }
}
