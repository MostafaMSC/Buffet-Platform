using BuffetDiscovery.Domain.Entities;

namespace BuffetDiscovery.Application.Common.Dtos;

/// One result card. Deliberately narrow: a card shows an image, who and where, the service
/// type, a price, a rating and whether the party can actually sit — anything more belongs
/// on the detail page.
public record ServiceCardDto(
    int ServiceId,
    ServiceType ServiceType,
    string Name,
    string NameAr,
    string? Description,
    string? DescriptionAr,

    int RestaurantId,
    string RestaurantName,
    string RestaurantNameAr,
    string AreaName,
    string AreaNameAr,
    string CityName,
    string CityNameAr,
    string CitySlug,
    double? Latitude,
    double? Longitude,

    string? PhotoUrl,
    MealType MealType,
    string[] Cuisines,
    string[] Dietary,

    PricingModel PricingModel,
    decimal Price,
    decimal? PriceChild,
    int? PackageGuests,
    string CurrencyCode,

    double? Rating,
    int ReviewCount,

    string OpensAt,
    string ClosesAt,
    int? DurationMinutes,
    int MinGuests,
    int? MaxGuests,

    bool IsAvailable,
    int? SpotsLeft,
    string? NextAvailableTime,
    bool BookingEnabled,
    BookingMode BookingMode,

    bool IsFoundingRestaurant,
    int RecentBookings
);

public record SearchResultsDto(
    int Total,
    int Page,
    int PageSize,
    List<ServiceCardDto> Items
);

public record CityCardDto(
    int Id,
    string Slug,
    string NameEn,
    string NameAr,
    string? ImageUrl,
    int ServiceCount
);

public record AreaOptionDto(int Id, string NameEn, string NameAr, string Slug);

public record CityOptionDto(int Id, string NameEn, string NameAr, string Slug, List<AreaOptionDto> Areas);

public record CountryOptionDto(int Id, string NameEn, string NameAr, string Code, string CurrencyCode, List<CityOptionDto> Cities);

/// Everything the homepage needs, in one request.
public record HomeFeedDto(
    List<ServiceCardDto> AvailableToday,
    List<ServiceCardDto> PopularBuffets,
    List<ServiceCardDto> PopularSetMenus,
    List<ServiceCardDto> Featured,
    List<CityCardDto> Cities
);
