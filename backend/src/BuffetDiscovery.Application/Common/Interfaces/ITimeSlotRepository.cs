using BuffetDiscovery.Domain.Entities;

namespace BuffetDiscovery.Application.Common.Interfaces;

public interface ITimeSlotRepository
{
    Task<List<TimeSlot>> GetByOfferingAsync(int offeringId, CancellationToken ct);
    Task<TimeSlot?> GetByIdForRestaurantAsync(int slotId, int restaurantId, CancellationToken ct);
    Task<TimeSlot?> GetByIdAsync(int slotId, CancellationToken ct);
    void Add(TimeSlot slot);
}
