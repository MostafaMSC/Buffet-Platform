using BuffetDiscovery.Api.Entities;

namespace BuffetDiscovery.Api.Dtos;

public record RestaurantProfileInputDto(
    string Name,
    string NameAr,
    int AreaId,
    string PhoneNumber,
    string? Address,
    string? GoogleMapsUrl,
    string? Description,
    string? DescriptionAr,
    string? LogoUrl,
    string? CoverPhotoUrl
);

public record RestaurantAdminListItemDto(
    int Id,
    string Name,
    string NameAr,
    string AreaNameEn,
    string PhoneNumber,
    RestaurantStatus Status,
    DateTime CreatedAt,
    int OfferingCount
);

public record SignupDto(
    string PhoneNumber,
    string Password,
    string RestaurantName,
    string RestaurantNameAr,
    int AreaId
);

public record LoginDto(string PhoneNumber, string Password);

public record AuthResponseDto(string Token, string Role, int? RestaurantId, RestaurantStatus? RestaurantStatus);
