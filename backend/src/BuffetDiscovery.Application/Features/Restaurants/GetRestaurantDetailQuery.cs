using BuffetDiscovery.Application.Common;
using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Application.Features.Search;
using MediatR;

namespace BuffetDiscovery.Application.Features.Restaurants;

/// A restaurant's own page — the venue, everything it currently offers, and its reviews.
public record GetRestaurantDetailQuery(int Id, DateOnly? Date) : IRequest<RestaurantPageDto?>;

public class GetRestaurantDetailQueryHandler(
    IRestaurantRepository restaurants,
    IServiceRepository services,
    ISearchRepository search,
    ISender mediator) : IRequestHandler<GetRestaurantDetailQuery, RestaurantPageDto?>
{
    public async Task<RestaurantPageDto?> Handle(GetRestaurantDetailQuery request, CancellationToken ct)
    {
        var restaurant = await restaurants.GetApprovedWithServicesAsync(request.Id, ct);
        if (restaurant is null) return null;

        var city = restaurant.Area!.City!;
        var ratings = await search.GetRatingsAsync([restaurant.Id], ct);
        var hasRating = ratings.TryGetValue(restaurant.Id, out var rating);

        // Reuse search so this restaurant's cards carry the same live availability, pricing
        // and badges they'd show anywhere else in the product.
        var results = await mediator.Send(new SearchServicesQuery(
            CitySlug: city.Slug,
            Date: request.Date,
            Availability: request.Date.HasValue ? AvailabilityWindow.SelectedDate : AvailabilityWindow.ThisWeek,
            Sort: SearchSort.Recommended,
            PageSize: 60), ct);

        var reviews = await services.GetReviewsAsync(restaurant.Id, null, 12, ct);

        return new RestaurantPageDto(
            new RestaurantSummaryDto(
                restaurant.Id,
                restaurant.Name,
                restaurant.NameAr,
                restaurant.Description,
                restaurant.DescriptionAr,
                restaurant.PhoneNumber,
                restaurant.Address,
                restaurant.GoogleMapsUrl,
                restaurant.Latitude,
                restaurant.Longitude,
                restaurant.LogoUrl,
                restaurant.CoverPhotoUrl,
                restaurant.Area!.NameEn,
                restaurant.Area!.NameAr,
                city.NameEn,
                city.NameAr,
                city.Slug,
                FlagEnums.Features(restaurant.Features),
                hasRating ? Math.Round(rating.Average, 1) : null,
                hasRating ? rating.Count : 0),
            results.Items.Where(s => s.RestaurantId == restaurant.Id).ToList(),
            reviews.Select(r => new ReviewDto(r.Id, r.CustomerName, r.Rating, r.Comment, r.CreatedAt, r.BookingId.HasValue)).ToList());
    }
}
