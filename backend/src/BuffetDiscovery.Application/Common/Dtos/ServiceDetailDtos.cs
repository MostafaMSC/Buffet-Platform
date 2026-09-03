using BuffetDiscovery.Domain.Entities;

namespace BuffetDiscovery.Application.Common.Dtos;

public record MenuItemDto(int Id, string Name, string NameAr, string? Description, string? DescriptionAr, string[] Dietary);

public record MenuSectionDto(int Id, string Name, string NameAr, List<MenuItemDto> Items);

public record ReviewDto(int Id, string CustomerName, int Rating, string? Comment, DateTime CreatedAt, bool IsVerified);

public record RestaurantSummaryDto(
    int Id,
    string Name,
    string NameAr,
    string? Description,
    string? DescriptionAr,
    string PhoneNumber,
    string? Address,
    string? GoogleMapsUrl,
    double? Latitude,
    double? Longitude,
    string? LogoUrl,
    string? CoverPhotoUrl,
    string AreaName,
    string AreaNameAr,
    string CityName,
    string CityNameAr,
    string CitySlug,
    string[] Features,
    double? Rating,
    int ReviewCount
);

/// What a slot looks like to a customer picking a time: how many seats are left, and
/// whether their party actually fits.
public record ServiceSlotDto(
    int? TimeSlotId,
    string StartTime,
    string EndTime,
    int Capacity,
    int Booked,
    int Remaining,
    bool IsFull,
    bool FitsParty,
    bool IsPast
);

public record ServiceAvailabilityDto(
    int ServiceId,
    DateOnly Date,
    bool IsServedOnDate,
    bool BookingEnabled,
    List<ServiceSlotDto> Slots
);

/// The price a specific party would pay, itemised the way the booking screen shows it.
public record PriceQuoteDto(
    PricingModel PricingModel,
    int Adults,
    int Children,
    decimal AdultUnitPrice,
    decimal ChildUnitPrice,
    decimal AdultsTotal,
    decimal ChildrenTotal,
    int? Packages,
    decimal? PackagePrice,
    decimal Total,
    string CurrencyCode
);

public record ServiceDetailDto(
    int Id,
    ServiceType ServiceType,
    string Name,
    string NameAr,
    string? Description,
    string? DescriptionAr,
    MealType MealType,
    string[] Cuisines,
    string[] Dietary,

    PricingModel PricingModel,
    decimal PricePerAdult,
    decimal? PricePerChild,
    int? ChildAgeFrom,
    int? ChildAgeTo,
    int? FreeUnderAge,
    decimal? PackagePrice,
    int? PackageGuests,
    string CurrencyCode,

    int MinGuests,
    int? MaxGuests,
    int? DurationMinutes,
    string OpensAt,
    string ClosesAt,
    RecurrenceType Recurrence,
    string[] Weekdays,
    DateOnly? RamadanStartDate,
    DateOnly? RamadanEndDate,
    DateOnly? OneOffDate,

    BookingMode BookingMode,
    int MinAdvanceMinutes,
    int CancellationCutoffMinutes,

    List<string> PhotoUrls,
    string? VideoUrl,
    List<MenuSectionDto> Menu,

    RestaurantSummaryDto Restaurant,
    ServiceAvailabilityDto Availability,
    PriceQuoteDto Quote,
    List<ReviewDto> Reviews,
    List<ServiceCardDto> SimilarServices
);
