using BuffetDiscovery.Domain.Entities;

namespace BuffetDiscovery.Application.Common.Interfaces;

public interface IWaitlistRepository
{
    /// Waiting/Offered entries for a slot (or whole offering window) on one date, ordered by position.
    Task<List<Waitlist>> GetQueueAsync(int? timeSlotId, int offeringId, DateOnly date, CancellationToken ct);

    Task<Waitlist?> GetByIdAsync(int id, CancellationToken ct);
    Task<Waitlist?> GetByIdForCustomerAsync(int id, string phone, CancellationToken ct);
    Task<List<Waitlist>> GetByPhoneAsync(string phone, CancellationToken ct);
    Task<int> GetNextPositionAsync(int? timeSlotId, int offeringId, DateOnly date, CancellationToken ct);

    void Add(Waitlist entry);
}
