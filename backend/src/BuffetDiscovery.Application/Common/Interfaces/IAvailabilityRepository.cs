using BuffetDiscovery.Domain.Entities;

namespace BuffetDiscovery.Application.Common.Interfaces;

public interface IAvailabilityRepository
{
    /// Existing AvailabilityStatus rows for the given services on one date, keyed by ServiceId.
    Task<Dictionary<int, AvailabilityStatus>> GetForDateAsync(IReadOnlyCollection<int> serviceIds, DateOnly date, CancellationToken ct);

    /// Existing AvailabilityStatus rows for the given services across a date range.
    Task<List<AvailabilityStatus>> GetForRangeAsync(IReadOnlyCollection<int> serviceIds, DateOnly start, DateOnly end, CancellationToken ct);

    Task<AvailabilityStatus?> GetAsync(int serviceId, DateOnly date, CancellationToken ct);

    void Add(AvailabilityStatus status);
}
