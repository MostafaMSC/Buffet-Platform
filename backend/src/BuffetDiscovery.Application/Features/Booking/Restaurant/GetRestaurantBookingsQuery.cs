using BuffetDiscovery.Application.Common;
using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Entities;
using MediatR;

namespace BuffetDiscovery.Application.Features.Booking.Restaurant;

/// The restaurant's booking dashboard: bookings grouped by (offering, time slot, date) so
/// staff see how full each service window is at a glance rather than a flat booking list.
public record GetRestaurantBookingsQuery(DateOnly? Date) : IRequest<List<RestaurantBookingGroupDto>>;

public class GetRestaurantBookingsQueryHandler(
    IBookingRepository bookingRepo,
    IRestaurantSettingsRepository settingsRepo,
    ICurrentUserService currentUser) : IRequestHandler<GetRestaurantBookingsQuery, List<RestaurantBookingGroupDto>>
{
    public async Task<List<RestaurantBookingGroupDto>> Handle(GetRestaurantBookingsQuery request, CancellationToken ct)
    {
        var restaurantId = currentUser.RestaurantId ?? throw new UnauthorizedException("No restaurant associated with this account.");
        var bookings = await bookingRepo.GetForRestaurantAsync(restaurantId, request.Date, ct);
        var settings = await settingsRepo.GetOrCreateAsync(restaurantId, ct);

        return bookings
            .GroupBy(b => (b.OfferingId, b.TimeSlotId, b.Date))
            .Select(g =>
            {
                var first = g.First();
                var offering = first.Offering!;
                var capacity = first.TimeSlot?.Capacity ?? offering.Capacity ?? 0;
                var effectiveCapacity = CapacityCalculator.EffectiveCapacity(capacity, settings.OverbookingTolerancePercent);
                var bookedPartySize = g.Where(b => b.Status == BookingStatus.Confirmed).Sum(b => b.PartySize);

                return new RestaurantBookingGroupDto(
                    first.OfferingId,
                    offering.MealType,
                    first.Date,
                    first.TimeSlotId,
                    first.TimeSlot?.StartTime.ToString("HH:mm") ?? offering.OpensAt.ToString("HH:mm"),
                    first.TimeSlot?.EndTime.ToString("HH:mm") ?? offering.ClosesAt.ToString("HH:mm"),
                    capacity,
                    effectiveCapacity,
                    bookedPartySize,
                    g.OrderBy(b => b.CreatedAt)
                        .Select(b => new RestaurantBookingListItemDto(b.Id, b.ConfirmationCode, b.CustomerName, b.CustomerPhone, b.PartySize, b.Status, b.CreatedAt))
                        .ToList()
                );
            })
            .OrderBy(x => x.Date).ThenBy(x => x.StartTime)
            .ToList();
    }
}
