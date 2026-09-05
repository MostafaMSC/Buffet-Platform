namespace BuffetDiscovery.Domain.Entities;

public class Waitlist
{
    public int Id { get; set; }

    public int ServiceId { get; set; }
    public Service? Service { get; set; }

    /// Null when the service isn't slot-divided, same nullable-with-service-fallback
    /// shape as Booking.TimeSlotId.
    public int? TimeSlotId { get; set; }
    public TimeSlot? TimeSlot { get; set; }

    public DateOnly Date { get; set; }

    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public int PartySize { get; set; }

    /// 1-based position in the queue for this (ServiceId/TimeSlotId, Date).
    public int Position { get; set; }

    public WaitlistStatus Status { get; set; } = WaitlistStatus.Waiting;

    /// When a spot opened up and this entry was offered it. Expires OfferWindowMinutes
    /// after this, at which point the offer lazily passes to the next person on read
    /// (same pattern as AvailabilityStatus's lazy per-date materialization — no
    /// background job infrastructure exists in this codebase yet).
    public DateTime? NotifiedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
