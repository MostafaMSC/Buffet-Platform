using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuffetDiscovery.Infrastructure.Persistence.Repositories;

public class AvailabilityRepository(AppDbContext db) : IAvailabilityRepository
{
    public async Task<Dictionary<int, AvailabilityStatus>> GetForDateAsync(IReadOnlyCollection<int> serviceIds, DateOnly date, CancellationToken ct)
    {
        var list = await db.AvailabilityStatuses
            .Where(a => serviceIds.Contains(a.ServiceId) && a.Date == date)
            .ToListAsync(ct);
        return list.ToDictionary(a => a.ServiceId, a => a);
    }

    public Task<List<AvailabilityStatus>> GetForRangeAsync(IReadOnlyCollection<int> serviceIds, DateOnly start, DateOnly end, CancellationToken ct) =>
        db.AvailabilityStatuses
            .Where(a => serviceIds.Contains(a.ServiceId) && a.Date >= start && a.Date <= end)
            .ToListAsync(ct);

    public Task<AvailabilityStatus?> GetAsync(int serviceId, DateOnly date, CancellationToken ct) =>
        db.AvailabilityStatuses.FirstOrDefaultAsync(a => a.ServiceId == serviceId && a.Date == date, ct);

    public void Add(AvailabilityStatus status) => db.AvailabilityStatuses.Add(status);
}
