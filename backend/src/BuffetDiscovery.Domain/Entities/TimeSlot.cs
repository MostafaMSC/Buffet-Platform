namespace BuffetDiscovery.Domain.Entities;

public class TimeSlot
{
    public int Id { get; set; }

    public int ServiceId { get; set; }
    public Service? Service { get; set; }

    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    /// Max total party size bookable for this slot on a given date.
    public int Capacity { get; set; }

    /// Minutes before StartTime during which new bookings are held back, giving the
    /// restaurant turnover time from the previous slot. 0 = no buffer (default).
    public int BufferMinutes { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Booking> Bookings { get; set; } = [];
    public List<Waitlist> WaitlistEntries { get; set; } = [];
    public List<SlotOverride> Overrides { get; set; } = [];
}
