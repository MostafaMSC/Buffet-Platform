using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Entities;
using MediatR;

namespace BuffetDiscovery.Application.Features.Booking.Restaurant;

/// The restaurant's opening screen: today's service, what's coming, and how the last 30
/// days went. Everything counted here comes from real bookings — a restaurant with no
/// history sees zeroes rather than invented numbers.
public record GetDashboardOverviewQuery : IRequest<DashboardOverviewDto>;

public class GetDashboardOverviewQueryHandler(
    IBookingRepository bookingRepo,
    ICurrentUserService currentUser) : IRequestHandler<GetDashboardOverviewQuery, DashboardOverviewDto>
{
    private static readonly BookingStatus[] Active =
        [BookingStatus.Confirmed, BookingStatus.Pending, BookingStatus.CheckedIn, BookingStatus.Completed];

    public async Task<DashboardOverviewDto> Handle(GetDashboardOverviewQuery request, CancellationToken ct)
    {
        var restaurantId = currentUser.RestaurantId ?? throw new UnauthorizedException("No restaurant associated with this account.");
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3));

        // One pull covering both the trailing 30 days and the next 30 keeps this to a
        // single query rather than one per tile.
        var bookings = await bookingRepo.GetForAnalyticsAsync(restaurantId, today.AddDays(-30), today.AddDays(30), ct);

        var todays = bookings.Where(b => b.Date == today && Active.Contains(b.Status)).ToList();
        var upcoming = bookings.Where(b => b.Date > today && Active.Contains(b.Status)).ToList();
        var past30 = bookings.Where(b => b.Date >= today.AddDays(-30) && b.Date <= today).ToList();

        var completed = past30.Count(b => b.Status == BookingStatus.Completed);
        var noShow = past30.Count(b => b.Status == BookingStatus.NoShow);
        var cancelled = past30.Count(b => b.Status is BookingStatus.Cancelled or BookingStatus.Rejected);
        var seated = completed + noShow;

        var topService = past30
            .Where(b => Active.Contains(b.Status))
            .GroupBy(b => b.ServiceId)
            .Select(g => new { Service = g.First().Service, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .FirstOrDefault();

        var byDate = Enumerable.Range(0, 14)
            .Select(offset => today.AddDays(-13 + offset))
            .Select(date =>
            {
                var forDate = past30.Where(b => b.Date == date && Active.Contains(b.Status)).ToList();
                return new DailyBookingStatDto(date, forDate.Sum(b => b.PartySize), forDate.Count);
            })
            .ToList();

        return new DashboardOverviewDto(
            today,
            todays.Count,
            todays.Sum(b => b.PartySize),
            bookings.Count(b => b.Status == BookingStatus.Pending && b.Date >= today),
            upcoming.Count,
            upcoming.Sum(b => b.PartySize),
            todays.Sum(b => b.TotalPrice),
            past30.Where(b => Active.Contains(b.Status)).Sum(b => b.TotalPrice),
            past30.Count(b => b.Service?.ServiceType == ServiceType.Buffet && Active.Contains(b.Status)),
            past30.Count(b => b.Service?.ServiceType == ServiceType.SetMenu && Active.Contains(b.Status)),
            seated == 0 ? 0 : Math.Round(100.0 * noShow / seated, 1),
            past30.Count == 0 ? 0 : Math.Round(100.0 * cancelled / past30.Count, 1),
            topService?.Service?.Name,
            topService?.Service?.NameAr,
            topService?.Count ?? 0,
            byDate);
    }
}
