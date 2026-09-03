namespace BuffetDiscovery.Domain.Entities;

public class Booking
{
    public int Id { get; set; }

    public int OfferingId { get; set; }
    public BuffetOffering? Offering { get; set; }

    /// Null when the offering isn't slot-divided — capacity is then checked against
    /// Offering.Capacity for (OfferingId, Date) directly.
    public int? TimeSlotId { get; set; }
    public TimeSlot? TimeSlot { get; set; }

    public DateOnly Date { get; set; }

    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public int PartySize { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Confirmed;

    /// Short unique code the customer uses to pull up their booking "badge" (a page with
    /// their booking details) without needing an account — this is also what a restaurant
    /// looks a booking up by at the door.
    public string ConfirmationCode { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CancelledAt { get; set; }
}
