using BuffetDiscovery.Domain.Entities;

namespace BuffetDiscovery.Application.Common.Dtos;

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

public record RestaurantProfileDto(
    int Id,
    string Name,
    string NameAr,
    int AreaId,
    string AreaNameEn,
    string PhoneNumber,
    string? Address,
    string? GoogleMapsUrl,
    string? Description,
    string? DescriptionAr,
    string? LogoUrl,
    string? CoverPhotoUrl,
    RestaurantStatus Status
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

public record AuthResponseDto(string Token, string Role, int? RestaurantId, RestaurantStatus? RestaurantStatus);

public record UploadResultDto(string Url);
