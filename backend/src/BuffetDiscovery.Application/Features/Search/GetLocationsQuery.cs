using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Common.Interfaces;
using MediatR;

namespace BuffetDiscovery.Application.Features.Search;

/// The full country → city → area tree, used by every location picker in the product.
public record GetLocationsQuery : IRequest<List<CountryOptionDto>>;

public class GetLocationsQueryHandler(ISearchRepository search)
    : IRequestHandler<GetLocationsQuery, List<CountryOptionDto>>
{
    public async Task<List<CountryOptionDto>> Handle(GetLocationsQuery request, CancellationToken ct)
    {
        var countries = await search.GetLocationTreeAsync(ct);

        return countries.Select(country => new CountryOptionDto(
            country.Id,
            country.NameEn,
            country.NameAr,
            country.Code,
            country.CurrencyCode,
            country.Cities.Select(city => new CityOptionDto(
                city.Id,
                city.NameEn,
                city.NameAr,
                city.Slug,
                city.Areas.Select(a => new AreaOptionDto(a.Id, a.NameEn, a.NameAr, a.Slug)).ToList()
            )).ToList()
        )).ToList();
    }
}
