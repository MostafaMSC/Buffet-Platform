using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Common.Interfaces;
using MediatR;

namespace BuffetDiscovery.Application.Features.Admin;

/// Admin's platform-wide booking volume view — not per-restaurant analytics (that's the
/// restaurant's own dashboard), just enough to see overall traction and daily trend.
public record GetPlatformBookingStatsQuery(DateOnly Start, DateOnly End) : IRequest<PlatformBookingStatsDto>;

public class GetPlatformBookingStatsQueryHandler(IBookingRepository bookingRepo)
    : IRequestHandler<GetPlatformBookingStatsQuery, PlatformBookingStatsDto>
{
    public async Task<PlatformBookingStatsDto> Handle(GetPlatformBookingStatsQuery request, CancellationToken ct)
    {
        var bookings = await bookingRepo.GetPlatformBookingsAsync(request.Start, request.End, ct);

        var byDate = bookings
            .GroupBy(b => b.Date)
            .Select(g => new DailyBookingStatDto(g.Key, g.Sum(b => b.PartySize), g.Count()))
            .OrderBy(x => x.Date)
            .ToList();

        var restaurantsWithBookings = bookings.Select(b => b.Service!.RestaurantId).Distinct().Count();

        return new PlatformBookingStatsDto(bookings.Count, bookings.Sum(b => b.PartySize), restaurantsWithBookings, byDate);
    }
}
