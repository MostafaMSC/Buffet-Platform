using System.Security.Claims;
using BuffetDiscovery.Application.Common.Interfaces;

namespace BuffetDiscovery.Api.Services;

/// Adapts the authenticated request's ClaimsPrincipal to ICurrentUserService. Lives in the
/// Api project (not Infrastructure) because IHttpContextAccessor is a web-hosting concern.
public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public int? UserId =>
        int.TryParse(User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public int? RestaurantId =>
        int.TryParse(User?.FindFirstValue("restaurantId"), out var id) ? id : null;

    public string? Role => User?.FindFirstValue(ClaimTypes.Role);
}
