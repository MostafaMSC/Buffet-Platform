using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuffetDiscovery.Infrastructure.Persistence.Repositories;

public class WaitlistRepository(AppDbContext db) : IWaitlistRepository
{
    public Task<List<Waitlist>> GetQueueAsync(int? timeSlotId, int serviceId, DateOnly date, CancellationToken ct)
    {
        var query = db.WaitlistEntries.Where(w =>
            w.ServiceId == serviceId && w.Date == date &&
            (w.Status == WaitlistStatus.Waiting || w.Status == WaitlistStatus.Offered));

        query = timeSlotId.HasValue
            ? query.Where(w => w.TimeSlotId == timeSlotId)
            : query.Where(w => w.TimeSlotId == null);

        return query.OrderBy(w => w.Position).ToListAsync(ct);
    }

    public Task<Waitlist?> GetByIdAsync(int id, CancellationToken ct) =>
        db.WaitlistEntries.Include(w => w.Service).Include(w => w.TimeSlot).FirstOrDefaultAsync(w => w.Id == id, ct);

    public Task<Waitlist?> GetByIdForCustomerAsync(int id, string phone, CancellationToken ct) =>
        db.WaitlistEntries
            .Include(w => w.Service)!.ThenInclude(o => o!.Restaurant)
            .Include(w => w.TimeSlot)
            .FirstOrDefaultAsync(w => w.Id == id && w.CustomerPhone == phone, ct);

    public Task<List<Waitlist>> GetByPhoneAsync(string phone, CancellationToken ct) =>
        db.WaitlistEntries
            .Include(w => w.Service)!.ThenInclude(o => o!.Restaurant)
            .Include(w => w.TimeSlot)
            .Where(w => w.CustomerPhone == phone)
            .OrderByDescending(w => w.Date)
            .ToListAsync(ct);

    public async Task<int> GetNextPositionAsync(int? timeSlotId, int serviceId, DateOnly date, CancellationToken ct)
    {
        var query = db.WaitlistEntries.Where(w =>
            w.ServiceId == serviceId && w.Date == date &&
            (w.Status == WaitlistStatus.Waiting || w.Status == WaitlistStatus.Offered));

        query = timeSlotId.HasValue
            ? query.Where(w => w.TimeSlotId == timeSlotId)
            : query.Where(w => w.TimeSlotId == null);

        var maxPosition = await query.Select(w => (int?)w.Position).MaxAsync(ct);
        return (maxPosition ?? 0) + 1;
    }

    public void Add(Waitlist entry) => db.WaitlistEntries.Add(entry);
}
