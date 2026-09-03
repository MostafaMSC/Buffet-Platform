using BuffetDiscovery.Domain.Entities;

namespace BuffetDiscovery.Application.Common.Dtos;

public record OfferingListItemDto(
    int OfferingId,
    int RestaurantId,
    string RestaurantName,
    string RestaurantNameAr,
    int AreaId,
    string AreaNameEn,
    string AreaNameAr,
    string? CoverPhotoUrl,
    MealType MealType,
    decimal Price,
    string OpensAt,
    string ClosesAt,
    bool IsFoundingRestaurant
);

public record RestaurantDetailDto(
    int Id,
    string Name,
    string NameAr,
    string AreaNameEn,
    string AreaNameAr,
    string PhoneNumber,
    string? Address,
    string? GoogleMapsUrl,
    string? Description,
    string? DescriptionAr,
    string? LogoUrl,
    string? CoverPhotoUrl,
    bool IsFoundingRestaurant,
    List<RestaurantOfferingDto> Offerings
);

public record RestaurantOfferingDto(
    int Id,
    MealType MealType,
    decimal Price,
    string OpensAt,
    string ClosesAt,
    string? Description,
    string? DescriptionAr,
    List<string> PhotoUrls,
    string? VideoUrl,
    bool IsActiveToday
);

public record DashboardOfferingDto(
    int Id,
    MealType MealType,
    decimal Price,
    string OpensAt,
    string ClosesAt,
    string? Description,
    string? DescriptionAr,
    RecurrenceType Recurrence,
    List<string> Weekdays,
    DateOnly? RamadanStartDate,
    DateOnly? RamadanEndDate,
    DateOnly? OneOffDate,
    List<string> PhotoUrls,
    string? VideoUrl,
    List<DayStatusDto> Days
);

public record DayStatusDto(DateOnly Date, bool IsActive);
