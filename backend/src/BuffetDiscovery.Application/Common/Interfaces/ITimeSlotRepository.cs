using BuffetDiscovery.Domain.Entities;

namespace BuffetDiscovery.Application.Common.Interfaces;

public interface ITimeSlotRepository
{
    Task<List<TimeSlot>> GetByServiceAsync(int serviceId, CancellationToken ct);
    Task<TimeSlot?> GetByIdForRestaurantAsync(int slotId, int restaurantId, CancellationToken ct);
    Task<TimeSlot?> GetByIdAsync(int slotId, CancellationToken ct);
    void Add(TimeSlot slot);
}
