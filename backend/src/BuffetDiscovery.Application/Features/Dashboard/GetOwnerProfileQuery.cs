using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using MediatR;

namespace BuffetDiscovery.Application.Features.Dashboard;

public record GetOwnerProfileQuery : IRequest<RestaurantProfileDto>;

public class GetOwnerProfileQueryHandler(
    IRestaurantRepository restaurants,
    ICurrentUserService currentUser) : IRequestHandler<GetOwnerProfileQuery, RestaurantProfileDto>
{
    public async Task<RestaurantProfileDto> Handle(GetOwnerProfileQuery request, CancellationToken ct)
    {
        var restaurantId = currentUser.RestaurantId ?? throw new UnauthorizedException("No restaurant associated with this account.");
        var r = await restaurants.GetByIdAsync(restaurantId, ct) ?? throw new NotFoundException("Restaurant not found.");

        return new RestaurantProfileDto(
            r.Id, r.Name, r.NameAr, r.AreaId, r.Area!.NameEn, r.PhoneNumber, r.Address, r.GoogleMapsUrl,
            r.Description, r.DescriptionAr, r.LogoUrl, r.CoverPhotoUrl, r.Status
        );
    }
}
