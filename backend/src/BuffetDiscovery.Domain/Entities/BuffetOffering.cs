namespace BuffetDiscovery.Domain.Entities;

public class BuffetOffering
{
    public int Id { get; set; }

    public int RestaurantId { get; set; }
    public Restaurant? Restaurant { get; set; }

    public MealType MealType { get; set; }
    public decimal Price { get; set; }
    public TimeOnly OpensAt { get; set; }
    public TimeOnly ClosesAt { get; set; }

    public string? Description { get; set; }
    public string? DescriptionAr { get; set; }

    /// Optional link to an external video (Facebook/YouTube/Instagram) showing the buffet spread,
    /// shown alongside uploaded photos. Not a hosted file — restaurants already post this content
    /// on their own social pages, so linking avoids us hosting/streaming video ourselves.
    public string? VideoUrl { get; set; }

    public RecurrenceType Recurrence { get; set; }
    public WeekDays Weekdays { get; set; } = WeekDays.None;
    public DateOnly? RamadanStartDate { get; set; }
    public DateOnly? RamadanEndDate { get; set; }
    public DateOnly? OneOffDate { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<OfferingPhoto> Photos { get; set; } = [];
    public List<AvailabilityStatus> AvailabilityStatuses { get; set; } = [];
}
