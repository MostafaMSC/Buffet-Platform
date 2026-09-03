using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Entities;
using BuffetDiscovery.Domain.Services;
using MediatR;

namespace BuffetDiscovery.Application.Features.Offerings;

public record BrowseOfferingsQuery(DateOnly? Date, int? AreaId, MealType? MealType) : IRequest<List<OfferingListItemDto>>;

/// Note: intentionally has a side effect. The first time a date is browsed, any offering whose
/// recurrence rule matches but has no explicit AvailabilityStatus row yet gets one materialized
/// (defaulting to active), so a restaurant can later override that specific day without the
/// materialization racing ahead of it. See AvailabilityStatus in the domain model.
public class BrowseOfferingsQueryHandler(
    IOfferingRepository offerings,
    IAvailabilityRepository availability,
    IRestaurantSettingsRepository settingsRepo,
    IUnitOfWork unitOfWork) : IRequestHandler<BrowseOfferingsQuery, List<OfferingListItemDto>>
{
    public async Task<List<OfferingListItemDto>> Handle(BrowseOfferingsQuery request, CancellationToken ct)
    {
        var targetDate = request.Date ?? DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3)); // Baghdad is UTC+3

        var candidates = await offerings.GetBrowseCandidatesAsync(request.AreaId, request.MealType, ct);
        var matching = candidates.Where(o => RecurrenceEvaluator.MatchesRecurrence(o, targetDate)).ToList();

        if (matching.Count == 0)
        {
            return [];
        }

        var offeringIds = matching.Select(o => o.Id).ToList();
        var overrides = await availability.GetForDateAsync(offeringIds, targetDate, ct);
        var restaurantIds = matching.Select(o => o.RestaurantId).Distinct().ToList();
        var settingsByRestaurant = await settingsRepo.GetForRestaurantsAsync(restaurantIds, ct);

        var result = new List<(OfferingListItemDto Dto, int FeaturedScore)>();

        foreach (var offering in matching)
        {
            bool isActive;
            if (overrides.TryGetValue(offering.Id, out var status))
            {
                isActive = status.IsActive;
            }
            else
            {
                isActive = true;
                availability.Add(new AvailabilityStatus { OfferingId = offering.Id, Date = targetDate, IsActive = true });
            }

            if (!isActive) continue;

            var restaurant = offering.Restaurant!;
            settingsByRestaurant.TryGetValue(restaurant.Id, out var settings);

            result.Add((
                new OfferingListItemDto(
                    offering.Id,
                    restaurant.Id,
                    restaurant.Name,
                    restaurant.NameAr,
                    restaurant.AreaId,
                    restaurant.Area!.NameEn,
                    restaurant.Area!.NameAr,
                    restaurant.CoverPhotoUrl,
                    offering.MealType,
                    offering.Price,
                    offering.OpensAt.ToString("HH:mm"),
                    offering.ClosesAt.ToString("HH:mm"),
                    settings?.IsFoundingRestaurant ?? false
                ),
                settings?.FeaturedScore ?? 0
            ));
        }

        await unitOfWork.SaveChangesAsync(ct);

        return result
            .OrderByDescending(r => r.FeaturedScore)
            .ThenBy(r => r.Dto.RestaurantName)
            .Select(r => r.Dto)
            .ToList();
    }
}
