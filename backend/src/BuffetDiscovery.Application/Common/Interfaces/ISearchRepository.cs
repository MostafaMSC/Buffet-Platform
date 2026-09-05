using BuffetDiscovery.Domain.Entities;

namespace BuffetDiscovery.Application.Common.Interfaces;

/// The DB-side half of a search: everything that can be narrowed with SQL is, so the
/// handler only ever evaluates recurrence and availability over a small candidate set.
public record ServiceSearchFilter(
    ServiceType? ServiceType = null,
    string? CitySlug = null,
    int? AreaId = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    Cuisines Cuisines = Domain.Entities.Cuisines.None,
    DietaryTags Dietary = DietaryTags.None,
    RestaurantFeatures Features = RestaurantFeatures.None,
    MealType[]? MealTypes = null,
    BookingMode? BookingMode = null,
    int? Guests = null,
    string? Query = null
);

public interface ISearchRepository
{
    /// Live services of approved restaurants matching the SQL-expressible filters, with
    /// restaurant, location, photos and slots loaded.
    Task<List<Service>> FindCandidatesAsync(ServiceSearchFilter filter, CancellationToken ct);

    /// Confirmed guest counts for one date keyed by (service, slot) — slot is null for
    /// whole-window services. One query for the whole result page.
    Task<Dictionary<(int ServiceId, int? TimeSlotId), int>> GetBookedGuestsAsync(
        IReadOnlyCollection<int> serviceIds, DateOnly date, CancellationToken ct);

    /// Per-date slot exceptions for the services on the page.
    Task<Dictionary<(int TimeSlotId, DateOnly Date), SlotOverride>> GetSlotOverridesAsync(
        IReadOnlyCollection<int> serviceIds, DateOnly from, DateOnly to, CancellationToken ct);

    /// Explicit per-date on/off switches set by restaurants, keyed by (service, date).
    Task<Dictionary<(int ServiceId, DateOnly Date), bool>> GetDayStatusesAsync(
        IReadOnlyCollection<int> serviceIds, DateOnly from, DateOnly to, CancellationToken ct);

    /// Average rating and review count per restaurant. Restaurants with no reviews are
    /// absent rather than zero, so callers can show "no rating yet" instead of "0.0".
    Task<Dictionary<int, (double Average, int Count)>> GetRatingsAsync(
        IReadOnlyCollection<int> restaurantIds, CancellationToken ct);

    /// Non-cancelled bookings per service over a recent window, used for popularity sort
    /// and the "booked N times" signal.
    Task<Dictionary<int, int>> GetRecentBookingCountsAsync(
        IReadOnlyCollection<int> serviceIds, DateOnly since, CancellationToken ct);

    /// Booking settings (overbooking tolerance, cancellation cutoff) for a set of
    /// restaurants, without materializing default rows.
    Task<Dictionary<int, RestaurantSettings>> GetSettingsAsync(
        IReadOnlyCollection<int> restaurantIds, CancellationToken ct);

    /// Cities with the number of live, bookable services in each — the count shown on the
    /// "discover by region" cards, so an empty city can be hidden rather than leading
    /// someone to a dead end.
    Task<List<(City City, int ServiceCount)>> GetCitiesWithCountsAsync(CancellationToken ct);

    Task<List<Country>> GetLocationTreeAsync(CancellationToken ct);
}
