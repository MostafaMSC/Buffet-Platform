using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuffetDiscovery.Infrastructure.Persistence.Repositories;

public class AvailabilityRepository(AppDbContext db) : IAvailabilityRepository
{
    public async Task<Dictionary<int, AvailabilityStatus>> GetForDateAsync(IReadOnlyCollection<int> offeringIds, DateOnly date, CancellationToken ct)
    {
        var list = await db.AvailabilityStatuses
            .Where(a => offeringIds.Contains(a.OfferingId) && a.Date == date)
            .ToListAsync(ct);
        return list.ToDictionary(a => a.OfferingId, a => a);
    }

    public Task<List<AvailabilityStatus>> GetForRangeAsync(IReadOnlyCollection<int> offeringIds, DateOnly start, DateOnly end, CancellationToken ct) =>
        db.AvailabilityStatuses
            .Where(a => offeringIds.Contains(a.OfferingId) && a.Date >= start && a.Date <= end)
            .ToListAsync(ct);

    public Task<AvailabilityStatus?> GetAsync(int offeringId, DateOnly date, CancellationToken ct) =>
        db.AvailabilityStatuses.FirstOrDefaultAsync(a => a.OfferingId == offeringId && a.Date == date, ct);

    public void Add(AvailabilityStatus status) => db.AvailabilityStatuses.Add(status);
}
