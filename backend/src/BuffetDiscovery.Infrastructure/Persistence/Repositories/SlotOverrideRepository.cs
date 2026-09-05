using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuffetDiscovery.Infrastructure.Persistence.Repositories;

public class SlotOverrideRepository(AppDbContext db) : ISlotOverrideRepository
{
    public Task<SlotOverride?> GetAsync(int timeSlotId, DateOnly date, CancellationToken ct) =>
        db.SlotOverrides.FirstOrDefaultAsync(o => o.TimeSlotId == timeSlotId && o.Date == date, ct);

    public void Add(SlotOverride slotOverride) => db.SlotOverrides.Add(slotOverride);

    public void Remove(SlotOverride slotOverride) => db.SlotOverrides.Remove(slotOverride);
}
