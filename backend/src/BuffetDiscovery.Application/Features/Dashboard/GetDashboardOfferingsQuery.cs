using BuffetDiscovery.Application.Common;
using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Entities;
using BuffetDiscovery.Domain.Services;
using MediatR;

namespace BuffetDiscovery.Application.Features.Dashboard;

public record GetDashboardOfferingsQuery(int Days = 14) : IRequest<List<DashboardOfferingDto>>;

/// Note: intentionally has a side effect, same rationale as BrowseOfferingsQuery — ensures an
/// AvailabilityStatus row exists for every offering across the requested date range so the
/// toggle grid always has something concrete to flip.
public class GetDashboardOfferingsQueryHandler(
    IOfferingRepository offerings,
    IAvailabilityRepository availability,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<GetDashboardOfferingsQuery, List<DashboardOfferingDto>>
{
    public async Task<List<DashboardOfferingDto>> Handle(GetDashboardOfferingsQuery request, CancellationToken ct)
    {
        var restaurantId = currentUser.RestaurantId ?? throw new UnauthorizedException("No restaurant associated with this account.");

        var restaurantOfferings = await offerings.GetByRestaurantAsync(restaurantId, ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3));
        var endDate = today.AddDays(request.Days - 1);
        var offeringIds = restaurantOfferings.Select(o => o.Id).ToList();

        var existing = await availability.GetForRangeAsync(offeringIds, today, endDate, ct);
        var existingKeys = existing.Select(a => (a.OfferingId, a.Date)).ToHashSet();

        foreach (var offering in restaurantOfferings)
        {
            for (var date = today; date <= endDate; date = date.AddDays(1))
            {
                if (existingKeys.Contains((offering.Id, date))) continue;

                availability.Add(new AvailabilityStatus
                {
                    OfferingId = offering.Id,
                    Date = date,
                    IsActive = RecurrenceEvaluator.MatchesRecurrence(offering, date)
                });
            }
        }
        await unitOfWork.SaveChangesAsync(ct);

        var statuses = await availability.GetForRangeAsync(offeringIds, today, endDate, ct);

        return restaurantOfferings.Select(o => new DashboardOfferingDto(
            o.Id,
            o.MealType,
            o.Price,
            o.OpensAt.ToString("HH:mm"),
            o.ClosesAt.ToString("HH:mm"),
            o.Description,
            o.DescriptionAr,
            o.Recurrence,
            WeekdayMapper.ToList(o.Weekdays),
            o.RamadanStartDate,
            o.RamadanEndDate,
            o.OneOffDate,
            o.Photos.OrderBy(p => p.SortOrder).Select(p => p.Url).ToList(),
            o.VideoUrl,
            statuses.Where(s => s.OfferingId == o.Id)
                .OrderBy(s => s.Date)
                .Select(s => new DayStatusDto(s.Date, s.IsActive))
                .ToList()
        )).ToList();
    }
}
