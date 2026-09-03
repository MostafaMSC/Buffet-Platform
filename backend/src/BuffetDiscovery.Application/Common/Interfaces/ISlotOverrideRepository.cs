using BuffetDiscovery.Domain.Entities;

namespace BuffetDiscovery.Application.Common.Interfaces;

public interface ISlotOverrideRepository
{
    Task<SlotOverride?> GetAsync(int timeSlotId, DateOnly date, CancellationToken ct);
    void Add(SlotOverride slotOverride);
    void Remove(SlotOverride slotOverride);
}
