using BuffetDiscovery.Application.Common;
using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Services;
using MediatR;

namespace BuffetDiscovery.Application.Features.Dashboard;

public record CalendarSlotDto(
    int? TimeSlotId,
    string StartTime,
    string EndTime,
    int Capacity,
    int Booked,
    int Remaining,
    bool IsBlocked,
    string? Note
);

public record CalendarServiceDayDto(
    int ServiceId,
    string ServiceName,
    string ServiceNameAr,
    bool IsServed,
    bool IsDayOn,
    List<CalendarSlotDto> Slots
);

public record CalendarDayDto(
    DateOnly Date,
    int TotalCapacity,
    int TotalBooked,
    List<CalendarServiceDayDto> Services
);

/// The restaurant's availability calendar: for each date in the range, every service that
/// runs that day and how full each sitting is.
public record GetCalendarQuery(DateOnly From, DateOnly To, int? ServiceId = null) : IRequest<List<CalendarDayDto>>;

public class GetCalendarQueryHandler(
    IServiceRepository services,
    ISearchRepository search,
    IRestaurantSettingsRepository settingsRepo,
    ICurrentUserService currentUser) : IRequestHandler<GetCalendarQuery, List<CalendarDayDto>>
{
    public async Task<List<CalendarDayDto>> Handle(GetCalendarQuery request, CancellationToken ct)
    {
        var restaurantId = currentUser.RestaurantId ?? throw new UnauthorizedException("No restaurant associated with this account.");

        var all = await services.GetByRestaurantAsync(restaurantId, ct);
        var list = request.ServiceId.HasValue
            ? all.Where(s => s.Id == request.ServiceId.Value).ToList()
            : all;

        if (list.Count == 0) return [];

        // Cap the window so a mis-typed range can't ask for years of days at once.
        var from = request.From;
        var to = request.To > from.AddDays(92) ? from.AddDays(92) : request.To;
        if (to < from) to = from;

        var ids = list.Select(s => s.Id).ToList();
        var settings = await settingsRepo.GetOrCreateAsync(restaurantId, ct);
        var overrides = await search.GetSlotOverridesAsync(ids, from, to, ct);
        var dayStatuses = await search.GetDayStatusesAsync(ids, from, to, ct);

        var days = new List<CalendarDayDto>();
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            var booked = await search.GetBookedGuestsAsync(ids, date, ct);
            var serviceDays = new List<CalendarServiceDayDto>();

            foreach (var service in list)
            {
                var isServed = RecurrenceEvaluator.MatchesRecurrence(service, date);
                var isDayOn = !dayStatuses.TryGetValue((service.Id, date), out var stored) || stored;
                if (!isServed) continue;

                var slots = AvailabilityCalculator.Build(service, date, booked, overrides, settings.OverbookingTolerancePercent);

                serviceDays.Add(new CalendarServiceDayDto(
                    service.Id,
                    service.Name,
                    service.NameAr,
                    isServed,
                    isDayOn,
                    slots.Select(s =>
                    {
                        var note = s.TimeSlotId.HasValue && overrides.TryGetValue((s.TimeSlotId.Value, date), out var ov) ? ov.Note : null;
                        return new CalendarSlotDto(
                            s.TimeSlotId,
                            s.StartTime.ToString("HH:mm"),
                            s.EndTime.ToString("HH:mm"),
                            s.Capacity,
                            s.Booked,
                            s.Remaining,
                            s.IsBlocked || !isDayOn,
                            note);
                    }).ToList()));
            }

            days.Add(new CalendarDayDto(
                date,
                serviceDays.Sum(s => s.Slots.Sum(x => x.Capacity)),
                serviceDays.Sum(s => s.Slots.Sum(x => x.Booked)),
                serviceDays));
        }

        return days;
    }
}
