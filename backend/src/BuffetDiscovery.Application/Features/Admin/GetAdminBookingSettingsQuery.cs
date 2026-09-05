using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Common.Interfaces;
using MediatR;

namespace BuffetDiscovery.Application.Features.Admin;

/// Admin view over every restaurant's booking settings — the overbooking tolerance they've
/// set (for moderation) alongside the platform-controlled incentive fields (founding badge,
/// featured ranking score, who referred them) that restaurants can't set themselves.
public record GetAdminBookingSettingsQuery : IRequest<List<AdminRestaurantSettingsDto>>;

public class GetAdminBookingSettingsQueryHandler(
    IRestaurantRepository restaurants,
    IRestaurantSettingsRepository settingsRepo) : IRequestHandler<GetAdminBookingSettingsQuery, List<AdminRestaurantSettingsDto>>
{
    public async Task<List<AdminRestaurantSettingsDto>> Handle(GetAdminBookingSettingsQuery request, CancellationToken ct)
    {
        var all = await restaurants.GetForAdminAsync(null, ct);
        var nameById = all.ToDictionary(r => r.Id, r => r.Name);

        var result = new List<AdminRestaurantSettingsDto>();
        foreach (var r in all)
        {
            var settings = await settingsRepo.GetOrCreateAsync(r.Id, ct);
            var referredByName = settings.ReferredByRestaurantId.HasValue && nameById.TryGetValue(settings.ReferredByRestaurantId.Value, out var n)
                ? n
                : null;

            result.Add(new AdminRestaurantSettingsDto(
                r.Id, r.Name, settings.CancellationCutoffMinutes, settings.OverbookingTolerancePercent,
                settings.IsFoundingRestaurant, settings.FeaturedScore, settings.ReferredByRestaurantId, referredByName
            ));
        }

        return result.OrderBy(x => x.RestaurantName).ToList();
    }
}
