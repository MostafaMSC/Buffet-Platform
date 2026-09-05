using BuffetDiscovery.Domain.Entities;

namespace BuffetDiscovery.Application.Common;

/// The single place a booking total is worked out, so the price quoted on the detail page,
/// the price shown at checkout and the price stored on the booking can't drift apart.
public static class PriceCalculator
{
    /// A per-person service charges adults and children separately; a package service
    /// charges a flat price per package, buying as many packages as the party needs.
    /// Children below the service's FreeUnderAge are not counted as paying children by the
    /// booking form at all, so they never reach this calculation.
    public static decimal Total(Service service, int adults, int children)
    {
        if (service.PricingModel == PricingModel.PerPackage)
        {
            var packagePrice = service.PackagePrice ?? 0;
            var packageGuests = service.PackageGuests is > 0 ? service.PackageGuests.Value : 1;
            var guests = Math.Max(adults + children, 1);
            var packages = (int)Math.Ceiling(guests / (double)packageGuests);
            return packagePrice * packages;
        }

        var childPrice = service.PricePerChild ?? service.PricePerAdult;
        return service.PricePerAdult * adults + childPrice * children;
    }

    /// The number shown on cards — what one adult pays, or the package price when the
    /// service is sold as a package.
    public static decimal HeadlinePrice(Service service) =>
        service.PricingModel == PricingModel.PerPackage
            ? service.PackagePrice ?? 0
            : service.PricePerAdult;
}
