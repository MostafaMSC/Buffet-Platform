using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Services;
using MediatR;

namespace BuffetDiscovery.Application.Features.Restaurants;

public record GetRestaurantDetailQuery(int Id, DateOnly? Date) : IRequest<RestaurantDetailDto?>;

public class GetRestaurantDetailQueryHandler(
    IRestaurantRepository restaurants,
    IAvailabilityRepository availability) : IRequestHandler<GetRestaurantDetailQuery, RestaurantDetailDto?>
{
    public async Task<RestaurantDetailDto?> Handle(GetRestaurantDetailQuery request, CancellationToken ct)
    {
        var restaurant = await restaurants.GetApprovedWithOfferingsAsync(request.Id, ct);
        if (restaurant is null) return null;

        var targetDate = request.Date ?? DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3));

        var offeringIds = restaurant.Offerings.Select(o => o.Id).ToList();
        var overrides = await availability.GetForDateAsync(offeringIds, targetDate, ct);

        var offeringDtos = restaurant.Offerings.Select(o =>
        {
            var matchesRecurrence = RecurrenceEvaluator.MatchesRecurrence(o, targetDate);
            var isActiveToday = overrides.TryGetValue(o.Id, out var status) ? status.IsActive : matchesRecurrence;

            return new RestaurantOfferingDto(
                o.Id,
                o.MealType,
                o.Price,
                o.OpensAt.ToString("HH:mm"),
                o.ClosesAt.ToString("HH:mm"),
                o.Description,
                o.DescriptionAr,
                o.Photos.OrderBy(p => p.SortOrder).Select(p => p.Url).ToList(),
                o.VideoUrl,
                isActiveToday
            );
        }).ToList();

        return new RestaurantDetailDto(
            restaurant.Id,
            restaurant.Name,
            restaurant.NameAr,
            restaurant.Area!.NameEn,
            restaurant.Area!.NameAr,
            restaurant.PhoneNumber,
            restaurant.Address,
            restaurant.GoogleMapsUrl,
            restaurant.Description,
            restaurant.DescriptionAr,
            restaurant.LogoUrl,
            restaurant.CoverPhotoUrl,
            offeringDtos
        );
    }
}
