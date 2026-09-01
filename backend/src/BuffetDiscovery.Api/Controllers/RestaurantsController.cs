using BuffetDiscovery.Api.Data;
using BuffetDiscovery.Api.Dtos;
using BuffetDiscovery.Api.Entities;
using BuffetDiscovery.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BuffetDiscovery.Api.Controllers;

[ApiController]
[Route("api/restaurants")]
public class RestaurantsController(AppDbContext db) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<RestaurantDetailDto>> GetById(int id, [FromQuery] DateOnly? date)
    {
        var restaurant = await db.Restaurants
            .Include(r => r.Area)
            .Include(r => r.Offerings.Where(o => !o.IsDeleted))
                .ThenInclude(o => o.Photos)
            .FirstOrDefaultAsync(r => r.Id == id && r.Status == RestaurantStatus.Approved);

        if (restaurant is null) return NotFound();

        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3));

        var offeringIds = restaurant.Offerings.Select(o => o.Id).ToList();
        var overrides = await db.AvailabilityStatuses
            .Where(a => offeringIds.Contains(a.OfferingId) && a.Date == targetDate)
            .ToDictionaryAsync(a => a.OfferingId, a => a.IsActive);

        var offeringDtos = restaurant.Offerings.Select(o =>
        {
            var matchesRecurrence = AvailabilityService.MatchesRecurrence(o, targetDate);
            var isActiveToday = overrides.TryGetValue(o.Id, out var overrideActive)
                ? overrideActive
                : matchesRecurrence;

            return new RestaurantOfferingDto(
                o.Id,
                o.MealType,
                o.Price,
                o.OpensAt.ToString("HH:mm"),
                o.ClosesAt.ToString("HH:mm"),
                o.Description,
                o.DescriptionAr,
                o.Photos.OrderBy(p => p.SortOrder).Select(p => p.Url).ToList(),
                isActiveToday
            );
        }).ToList();

        return Ok(new RestaurantDetailDto(
            restaurant.Id,
            restaurant.Name,
            restaurant.NameAr,
            restaurant.Area!.NameEn,
            restaurant.Area!.NameAr,
            restaurant.PhoneNumber,
            restaurant.Address,
            restaurant.GoogleMapsUrl,
            restaurant.Description,
            restaurant.DescriptionAr,
            restaurant.LogoUrl,
            restaurant.CoverPhotoUrl,
            offeringDtos
        ));
    }
}
