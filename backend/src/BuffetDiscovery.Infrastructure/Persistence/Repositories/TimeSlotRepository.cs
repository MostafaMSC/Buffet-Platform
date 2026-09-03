using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuffetDiscovery.Infrastructure.Persistence.Repositories;

public class TimeSlotRepository(AppDbContext db) : ITimeSlotRepository
{
    public Task<List<TimeSlot>> GetByServiceAsync(int serviceId, CancellationToken ct) =>
        db.TimeSlots
            .Where(s => s.ServiceId == serviceId && !s.IsDeleted)
            .OrderBy(s => s.StartTime)
            .ToListAsync(ct);

    public Task<TimeSlot?> GetByIdForRestaurantAsync(int slotId, int restaurantId, CancellationToken ct) =>
        db.TimeSlots
            .Include(s => s.Service)
            .FirstOrDefaultAsync(s => s.Id == slotId && !s.IsDeleted && s.Service!.RestaurantId == restaurantId, ct);

    public Task<TimeSlot?> GetByIdAsync(int slotId, CancellationToken ct) =>
        db.TimeSlots.FirstOrDefaultAsync(s => s.Id == slotId && !s.IsDeleted, ct);

    public void Add(TimeSlot slot) => db.TimeSlots.Add(slot);
}
