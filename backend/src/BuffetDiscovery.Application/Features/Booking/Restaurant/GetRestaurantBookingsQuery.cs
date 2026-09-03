using BuffetDiscovery.Application.Common;
using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Entities;
using MediatR;

namespace BuffetDiscovery.Application.Features.Booking.Restaurant;

/// The restaurant's booking list, grouped by (service, sitting, date) so staff see how full
/// each service window is at a glance rather than a flat list of names.
public record GetRestaurantBookingsQuery(DateOnly? Date, BookingStatus? Status = null)
    : IRequest<List<RestaurantBookingGroupDto>>;

public class GetRestaurantBookingsQueryHandler(
    IBookingRepository bookingRepo,
    IRestaurantSettingsRepository settingsRepo,
    ICurrentUserService currentUser) : IRequestHandler<GetRestaurantBookingsQuery, List<RestaurantBookingGroupDto>>
{
    private static readonly BookingStatus[] SeatHolding =
        [BookingStatus.Confirmed, BookingStatus.Pending, BookingStatus.CheckedIn];

    public async Task<List<RestaurantBookingGroupDto>> Handle(GetRestaurantBookingsQuery request, CancellationToken ct)
    {
        var restaurantId = currentUser.RestaurantId ?? throw new UnauthorizedException("No restaurant associated with this account.");
        var bookings = await bookingRepo.GetForRestaurantAsync(restaurantId, request.Date, ct);
        var settings = await settingsRepo.GetOrCreateAsync(restaurantId, ct);

        if (request.Status.HasValue)
        {
            bookings = bookings.Where(b => b.Status == request.Status.Value).ToList();
        }

        return bookings
            .GroupBy(b => (b.ServiceId, b.TimeSlotId, b.Date))
            .Select(g =>
            {
                var first = g.First();
                var service = first.Service!;
                var capacity = first.TimeSlot?.Capacity ?? service.Capacity ?? 0;

                return new RestaurantBookingGroupDto(
                    first.ServiceId,
                    service.Name,
                    service.NameAr,
                    service.ServiceType,
                    service.MealType,
                    first.Date,
                    first.TimeSlotId,
                    first.TimeSlot?.StartTime.ToString("HH:mm") ?? service.OpensAt.ToString("HH:mm"),
                    first.TimeSlot?.EndTime.ToString("HH:mm") ?? service.ClosesAt.ToString("HH:mm"),
                    capacity,
                    CapacityCalculator.EffectiveCapacity(capacity, settings.OverbookingTolerancePercent),
                    g.Where(b => SeatHolding.Contains(b.Status)).Sum(b => b.PartySize),
                    g.OrderBy(b => b.CreatedAt)
                        .Select(b => new RestaurantBookingListItemDto(
                            b.Id, b.ConfirmationCode, b.CustomerName, b.CustomerPhone, b.CustomerEmail,
                            b.SpecialRequests, b.PartySize, b.Adults, b.Children, b.TotalPrice, b.Status, b.CreatedAt))
                        .ToList());
            })
            .OrderBy(x => x.Date).ThenBy(x => x.StartTime)
            .ToList();
    }
}
