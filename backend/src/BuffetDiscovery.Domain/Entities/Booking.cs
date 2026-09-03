namespace BuffetDiscovery.Domain.Entities;

public class Booking
{
    public int Id { get; set; }

    public int ServiceId { get; set; }
    public Service? Service { get; set; }

    /// Null when the service isn't slot-divided — capacity is then checked against
    /// Service.Capacity for (ServiceId, Date) directly.
    public int? TimeSlotId { get; set; }
    public TimeSlot? TimeSlot { get; set; }

    public DateOnly Date { get; set; }

    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string? SpecialRequests { get; set; }

    /// Total head count — always Adults + Children, kept denormalized because every
    /// capacity check sums it.
    public int PartySize { get; set; }
    public int Adults { get; set; } = 1;
    public int Children { get; set; }

    /// Price quoted at the time of booking, so later price changes don't rewrite history.
    public decimal TotalPrice { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Confirmed;

    /// Short unique code the customer uses to pull up their booking "badge" (a page with
    /// their booking details) without needing an account — this is also what a restaurant
    /// looks a booking up by at the door. Formatted BUF-XXXXX / SET-XXXXX by service type.
    public string ConfirmationCode { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CancelledAt { get; set; }
    public DateTime? CheckedInAt { get; set; }
}
