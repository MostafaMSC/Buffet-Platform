using BuffetDiscovery.Domain.Entities;

namespace BuffetDiscovery.Application.Common.Interfaces;

public interface IAvailabilityRepository
{
    /// Existing AvailabilityStatus rows for the given offerings on one date, keyed by OfferingId.
    Task<Dictionary<int, AvailabilityStatus>> GetForDateAsync(IReadOnlyCollection<int> offeringIds, DateOnly date, CancellationToken ct);

    /// Existing AvailabilityStatus rows for the given offerings across a date range.
    Task<List<AvailabilityStatus>> GetForRangeAsync(IReadOnlyCollection<int> offeringIds, DateOnly start, DateOnly end, CancellationToken ct);

    Task<AvailabilityStatus?> GetAsync(int offeringId, DateOnly date, CancellationToken ct);

    void Add(AvailabilityStatus status);
}
