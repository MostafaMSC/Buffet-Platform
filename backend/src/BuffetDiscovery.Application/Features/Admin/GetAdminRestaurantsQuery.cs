using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Entities;
using MediatR;

namespace BuffetDiscovery.Application.Features.Admin;

public record GetAdminRestaurantsQuery(RestaurantStatus? Status) : IRequest<List<RestaurantAdminListItemDto>>;

public class GetAdminRestaurantsQueryHandler(IRestaurantRepository restaurants)
    : IRequestHandler<GetAdminRestaurantsQuery, List<RestaurantAdminListItemDto>>
{
    public async Task<List<RestaurantAdminListItemDto>> Handle(GetAdminRestaurantsQuery request, CancellationToken ct)
    {
        var all = await restaurants.GetForAdminAsync(request.Status, ct);

        return all.Select(r => new RestaurantAdminListItemDto(
            r.Id, r.Name, r.NameAr, r.Area!.NameEn, r.PhoneNumber, r.Status, r.CreatedAt,
            r.Offerings.Count(o => !o.IsDeleted)
        )).ToList();
    }
}
