using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Entities;
using MediatR;

namespace BuffetDiscovery.Application.Features.Search;

/// The homepage in a single request. Each row is a real search under the hood — "available
/// today" genuinely means bookable today — rather than a separately-maintained feed that
/// could drift out of step with what search returns.
public record GetHomeFeedQuery(string? CitySlug = null) : IRequest<HomeFeedDto>;

public class GetHomeFeedQueryHandler(ISearchRepository search, ISender mediator)
    : IRequestHandler<GetHomeFeedQuery, HomeFeedDto>
{
    private const int RowSize = 8;

    public async Task<HomeFeedDto> Handle(GetHomeFeedQuery request, CancellationToken ct)
    {
        var availableToday = await Row(new SearchServicesQuery(
            CitySlug: request.CitySlug,
            Availability: AvailabilityWindow.Today,
            Sort: SearchSort.Recommended,
            PageSize: RowSize), ct);

        var buffets = await Row(new SearchServicesQuery(
            Type: ServiceType.Buffet,
            CitySlug: request.CitySlug,
            Availability: AvailabilityWindow.ThisWeek,
            Sort: SearchSort.Popular,
            PageSize: RowSize), ct);

        var setMenus = await Row(new SearchServicesQuery(
            Type: ServiceType.SetMenu,
            CitySlug: request.CitySlug,
            Availability: AvailabilityWindow.ThisWeek,
            Sort: SearchSort.Popular,
            PageSize: RowSize), ct);

        var featured = await Row(new SearchServicesQuery(
            CitySlug: request.CitySlug,
            Availability: AvailabilityWindow.ThisWeek,
            Sort: SearchSort.Rating,
            PageSize: RowSize), ct);

        var cityCards = (await search.GetCitiesWithCountsAsync(ct))
            .Where(c => c.ServiceCount > 0)
            .Select(c => new CityCardDto(c.City.Id, c.City.Slug, c.City.NameEn, c.City.NameAr, c.City.ImageUrl, c.ServiceCount))
            .ToList();

        return new HomeFeedDto(availableToday, buffets, setMenus, featured, cityCards);
    }

    private async Task<List<ServiceCardDto>> Row(SearchServicesQuery query, CancellationToken ct) =>
        (await mediator.Send(query, ct)).Items;
}
