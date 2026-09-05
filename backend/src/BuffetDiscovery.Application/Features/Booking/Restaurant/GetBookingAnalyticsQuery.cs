using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Entities;
using MediatR;

namespace BuffetDiscovery.Application.Features.Booking.Restaurant;

public record GetBookingAnalyticsQuery(DateOnly Start, DateOnly End) : IRequest<BookingAnalyticsDto>;

public class GetBookingAnalyticsQueryHandler(
    IBookingRepository bookingRepo,
    ICurrentUserService currentUser) : IRequestHandler<GetBookingAnalyticsQuery, BookingAnalyticsDto>
{
    public async Task<BookingAnalyticsDto> Handle(GetBookingAnalyticsQuery request, CancellationToken ct)
    {
        var restaurantId = currentUser.RestaurantId ?? throw new UnauthorizedException("No restaurant associated with this account.");
        var bookings = await bookingRepo.GetForAnalyticsAsync(restaurantId, request.Start, request.End, ct);

        var completed = bookings.Count(b => b.Status == BookingStatus.Completed);
        var noShow = bookings.Count(b => b.Status == BookingStatus.NoShow);
        var cancelled = bookings.Count(b => b.Status == BookingStatus.Cancelled);
        var seatedOrNoShow = completed + noShow;
        var noShowRate = seatedOrNoShow == 0 ? 0 : Math.Round(100.0 * noShow / seatedOrNoShow, 1);

        var counted = bookings.Where(b => b.Status is BookingStatus.Confirmed or BookingStatus.Completed or BookingStatus.NoShow).ToList();

        var byDate = counted
            .GroupBy(b => b.Date)
            .Select(g => new DailyBookingStatDto(g.Key, g.Sum(b => b.PartySize), g.Count()))
            .OrderBy(x => x.Date)
            .ToList();

        var bySlot = counted
            .GroupBy(b => b.TimeSlotId)
            .Select(g =>
            {
                var first = g.First();
                var label = first.TimeSlot is not null
                    ? $"{first.TimeSlot.StartTime:HH:mm}-{first.TimeSlot.EndTime:HH:mm}"
                    : "Whole window";
                return new SlotBookingStatDto(g.Key, label, g.Sum(b => b.PartySize), g.Count());
            })
            .OrderBy(x => x.Label)
            .ToList();

        return new BookingAnalyticsDto(counted.Count, completed, noShow, cancelled, noShowRate, byDate, bySlot);
    }
}
