namespace BuffetDiscovery.Domain.Entities;

/// One bookable dining offer — a named Buffet or Set Menu. A restaurant can run several
/// (e.g. "Friday Family Buffet", "Seafood Buffet", "Business Lunch"), each with its own
/// pricing, menu, schedule and capacity.
public class Service
{
    public int Id { get; set; }

    public int RestaurantId { get; set; }
    public Restaurant? Restaurant { get; set; }

    public ServiceType ServiceType { get; set; }

    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;

    public string? Description { get; set; }
    public string? DescriptionAr { get; set; }

    public MealType MealType { get; set; }
    public Cuisines Cuisines { get; set; } = Cuisines.None;
    public DietaryTags Dietary { get; set; } = DietaryTags.None;

    // ---------- Pricing ----------

    public PricingModel PricingModel { get; set; } = PricingModel.PerPerson;

    /// Per-person price for an adult. For PerPackage services this is left at 0 and
    /// PackagePrice is used instead.
    public decimal PricePerAdult { get; set; }

    /// Optional reduced price for children within ChildAgeFrom..ChildAgeTo.
    public decimal? PricePerChild { get; set; }

    public int? ChildAgeFrom { get; set; }
    public int? ChildAgeTo { get; set; }

    /// Children below this age eat free. Null when no free-child policy applies.
    public int? FreeUnderAge { get; set; }

    /// Flat price covering PackageGuests people, e.g. 35,000 IQD for 2.
    public decimal? PackagePrice { get; set; }
    public int? PackageGuests { get; set; }

    // ---------- Guests & duration ----------

    public int MinGuests { get; set; } = 1;
    public int? MaxGuests { get; set; }

    /// How long a sitting lasts, used to describe the experience ("3 hours") and to space
    /// out slot suggestions.
    public int? DurationMinutes { get; set; }

    // ---------- Schedule ----------

    public TimeOnly OpensAt { get; set; }
    public TimeOnly ClosesAt { get; set; }

    public RecurrenceType Recurrence { get; set; }
    public WeekDays Weekdays { get; set; } = WeekDays.None;
    public DateOnly? RamadanStartDate { get; set; }
    public DateOnly? RamadanEndDate { get; set; }
    public DateOnly? OneOffDate { get; set; }

    // ---------- Booking rules ----------

    public BookingMode BookingMode { get; set; } = BookingMode.Instant;

    /// How far ahead of the sitting a booking must be made, in minutes.
    public int MinAdvanceMinutes { get; set; }

    /// Overrides the restaurant-wide cancellation cutoff when set.
    public int? CancellationCutoffMinutes { get; set; }

    // ---------- Capacity ----------

    /// Whole-window capacity (max total guests across the whole OpensAt–ClosesAt window on
    /// one date), used only when the service has no TimeSlots. Null means this service is
    /// not accepting bookings.
    public int? Capacity { get; set; }

    // ---------- Media & state ----------

    /// Optional video of the spread — an external link (Facebook/YouTube/Instagram) or an
    /// uploaded file served from /uploads.
    public string? VideoUrl { get; set; }

    public ServiceStatus Status { get; set; } = ServiceStatus.Active;

    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<ServicePhoto> Photos { get; set; } = [];
    public List<MenuSection> MenuSections { get; set; } = [];
    public List<AvailabilityStatus> AvailabilityStatuses { get; set; } = [];
    public List<TimeSlot> TimeSlots { get; set; } = [];
    public List<Booking> Bookings { get; set; } = [];
    public List<Review> Reviews { get; set; } = [];
}
