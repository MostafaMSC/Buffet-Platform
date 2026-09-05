namespace BuffetDiscovery.Application.Common.Interfaces;

/// Abstracts the authenticated caller's identity (from the HTTP context / JWT claims)
/// away from the web layer so Application handlers don't depend on ASP.NET Core.
public interface ICurrentUserService
{
    int? UserId { get; }
    int? RestaurantId { get; }
    string? Role { get; }
}
