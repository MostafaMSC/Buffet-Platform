using BuffetDiscovery.Domain.Entities;

namespace BuffetDiscovery.Application.Common.Dtos;

/// A restaurant's public page: who they are, plus every live service they run.
public record RestaurantPageDto(
    RestaurantSummaryDto Restaurant,
    List<ServiceCardDto> Services,
    List<ReviewDto> Reviews
);

public record DayStatusDto(DateOnly Date, bool IsActive);

/// One row in the restaurant's own service list — enough to see what a service is, how it
/// is priced, whether it is live, and how the coming week looks.
public record DashboardServiceDto(
    int Id,
    ServiceType ServiceType,
    string Name,
    string NameAr,
    string? Description,
    string? DescriptionAr,
    MealType MealType,
    ServiceStatus Status,
    PricingModel PricingModel,
    decimal PricePerAdult,
    decimal? PricePerChild,
    decimal? PackagePrice,
    int? PackageGuests,
    int MinGuests,
    int? MaxGuests,
    int? DurationMinutes,
    string OpensAt,
    string ClosesAt,
    RecurrenceType Recurrence,
    List<string> Weekdays,
    DateOnly? RamadanStartDate,
    DateOnly? RamadanEndDate,
    DateOnly? OneOffDate,
    BookingMode BookingMode,
    int? Capacity,
    int SlotCount,
    string[] Cuisines,
    string[] Dietary,
    List<string> PhotoUrls,
    string? VideoUrl,
    int MenuSectionCount,
    List<DayStatusDto> Days
);

/// The full editable shape of one service, as the restaurant's service editor loads it.
public record ServiceEditorDto(
    int Id,
    ServiceType ServiceType,
    string Name,
    string NameAr,
    string? Description,
    string? DescriptionAr,
    MealType MealType,
    string[] Cuisines,
    string[] Dietary,
    ServiceStatus Status,

    PricingModel PricingModel,
    decimal PricePerAdult,
    decimal? PricePerChild,
    int? ChildAgeFrom,
    int? ChildAgeTo,
    int? FreeUnderAge,
    decimal? PackagePrice,
    int? PackageGuests,

    int MinGuests,
    int? MaxGuests,
    int? DurationMinutes,

    string OpensAt,
    string ClosesAt,
    RecurrenceType Recurrence,
    List<string> Weekdays,
    DateOnly? RamadanStartDate,
    DateOnly? RamadanEndDate,
    DateOnly? OneOffDate,

    BookingMode BookingMode,
    int MinAdvanceMinutes,
    int? CancellationCutoffMinutes,
    int? Capacity,

    List<string> PhotoUrls,
    string? VideoUrl,
    List<TimeSlotDto> Slots,
    List<MenuSectionDto> Menu
);
