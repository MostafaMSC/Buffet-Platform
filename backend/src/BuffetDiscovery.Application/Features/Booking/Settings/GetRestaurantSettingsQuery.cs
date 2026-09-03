using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using MediatR;

namespace BuffetDiscovery.Application.Features.Booking.Settings;

public record GetRestaurantSettingsQuery : IRequest<RestaurantSettingsDto>;

public class GetRestaurantSettingsQueryHandler(
    IRestaurantSettingsRepository settingsRepo,
    ICurrentUserService currentUser) : IRequestHandler<GetRestaurantSettingsQuery, RestaurantSettingsDto>
{
    public async Task<RestaurantSettingsDto> Handle(GetRestaurantSettingsQuery request, CancellationToken ct)
    {
        var restaurantId = currentUser.RestaurantId ?? throw new UnauthorizedException("No restaurant associated with this account.");
        var settings = await settingsRepo.GetOrCreateAsync(restaurantId, ct);

        return new RestaurantSettingsDto(
            settings.CancellationCutoffMinutes,
            settings.WaitlistOfferWindowMinutes,
            settings.OverbookingTolerancePercent,
            settings.IsFoundingRestaurant,
            settings.ReferredByRestaurantId,
            settings.FeaturedScore
        );
    }
}
