namespace BuffetDiscovery.Domain.Entities;

/// A one-date exception to a time slot's normal capacity — the restaurant blocking 7:00 PM
/// on a holiday, or opening extra seats for a big Friday. Absent means the slot runs at its
/// standard capacity, so the calendar only stores the days that actually differ.
public class SlotOverride
{
    public int Id { get; set; }

    public int TimeSlotId { get; set; }
    public TimeSlot? TimeSlot { get; set; }

    public DateOnly Date { get; set; }

    /// Blocked slots take no bookings that day regardless of capacity.
    public bool IsBlocked { get; set; }

    /// Replaces the slot's own capacity for this date when set.
    public int? Capacity { get; set; }

    public string? Note { get; set; }
}
