using BuffetDiscovery.Application.Common;
using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Services;
using MediatR;

namespace BuffetDiscovery.Application.Features.Dashboard;

/// The restaurant's own service list — including paused and draft services, which never
/// appear in public search.
public record GetDashboardServicesQuery(int Days = 14) : IRequest<List<DashboardServiceDto>>;

public class GetDashboardServicesQueryHandler(
    IServiceRepository services,
    IAvailabilityRepository availability,
    ICurrentUserService currentUser) : IRequestHandler<GetDashboardServicesQuery, List<DashboardServiceDto>>
{
    public async Task<List<DashboardServiceDto>> Handle(GetDashboardServicesQuery request, CancellationToken ct)
    {
        var restaurantId = currentUser.RestaurantId ?? throw new UnauthorizedException("No restaurant associated with this account.");
        var list = await services.GetByRestaurantAsync(restaurantId, ct);
        if (list.Count == 0) return [];

        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3));
        var end = today.AddDays(Math.Clamp(request.Days, 1, 60) - 1);
        var overrides = await availability.GetForRangeAsync(list.Select(s => s.Id).ToList(), today, end, ct);
        var overrideLookup = overrides.ToDictionary(o => (o.ServiceId, o.Date), o => o.IsActive);

        return list.Select(service =>
        {
            var days = new List<DayStatusDto>();
            for (var date = today; date <= end; date = date.AddDays(1))
            {
                var matches = RecurrenceEvaluator.MatchesRecurrence(service, date);
                var isActive = overrideLookup.TryGetValue((service.Id, date), out var stored) ? stored : matches;
                days.Add(new DayStatusDto(date, matches && isActive));
            }

            return new DashboardServiceDto(
                service.Id,
                service.ServiceType,
                service.Name,
                service.NameAr,
                service.Description,
                service.DescriptionAr,
                service.MealType,
                service.Status,
                service.PricingModel,
                service.PricePerAdult,
                service.PricePerChild,
                service.PackagePrice,
                service.PackageGuests,
                service.MinGuests,
                service.MaxGuests,
                service.DurationMinutes,
                service.OpensAt.ToString("HH:mm"),
                service.ClosesAt.ToString("HH:mm"),
                service.Recurrence,
                WeekdayMapper.ToList(service.Weekdays),
                service.RamadanStartDate,
                service.RamadanEndDate,
                service.OneOffDate,
                service.BookingMode,
                service.Capacity,
                service.TimeSlots.Count(s => !s.IsDeleted),
                FlagEnums.Cuisines(service.Cuisines),
                FlagEnums.Dietary(service.Dietary),
                service.Photos.OrderBy(p => p.SortOrder).Select(p => p.Url).ToList(),
                service.VideoUrl,
                service.MenuSections.Count,
                days);
        }).ToList();
    }
}
