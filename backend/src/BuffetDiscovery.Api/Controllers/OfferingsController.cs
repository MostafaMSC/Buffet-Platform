using BuffetDiscovery.Api.Data;
using BuffetDiscovery.Api.Dtos;
using BuffetDiscovery.Api.Entities;
using BuffetDiscovery.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BuffetDiscovery.Api.Controllers;

[ApiController]
[Route("api/offerings")]
public class OfferingsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<OfferingListItemDto>>> Browse(
        [FromQuery] DateOnly? date,
        [FromQuery] int? areaId,
        [FromQuery] MealType? mealType)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3)); // Baghdad is UTC+3

        var query = db.Offerings
            .Include(o => o.Restaurant)!.ThenInclude(r => r!.Area)
            .Where(o => !o.IsDeleted && o.Restaurant!.Status == RestaurantStatus.Approved);

        if (areaId.HasValue)
        {
            query = query.Where(o => o.Restaurant!.AreaId == areaId.Value);
        }

        if (mealType.HasValue)
        {
            query = query.Where(o => o.MealType == mealType.Value);
        }

        var candidates = await query.ToListAsync();
        var matching = candidates.Where(o => AvailabilityService.MatchesRecurrence(o, targetDate)).ToList();

        if (matching.Count == 0)
        {
            return Ok(new List<OfferingListItemDto>());
        }

        var offeringIds = matching.Select(o => o.Id).ToList();
        var overrides = await db.AvailabilityStatuses
            .Where(a => offeringIds.Contains(a.OfferingId) && a.Date == targetDate)
            .ToDictionaryAsync(a => a.OfferingId, a => a);

        var result = new List<OfferingListItemDto>();

        foreach (var offering in matching)
        {
            bool isActive;
            if (overrides.TryGetValue(offering.Id, out var status))
            {
                isActive = status.IsActive;
            }
            else
            {
                isActive = true;
                db.AvailabilityStatuses.Add(new AvailabilityStatus
                {
                    OfferingId = offering.Id,
                    Date = targetDate,
                    IsActive = true
                });
            }

            if (!isActive) continue;

            var restaurant = offering.Restaurant!;
            result.Add(new OfferingListItemDto(
                offering.Id,
                restaurant.Id,
                restaurant.Name,
                restaurant.NameAr,
                restaurant.AreaId,
                restaurant.Area!.NameEn,
                restaurant.Area!.NameAr,
                restaurant.CoverPhotoUrl,
                offering.MealType,
                offering.Price,
                offering.OpensAt.ToString("HH:mm"),
                offering.ClosesAt.ToString("HH:mm")
            ));
        }

        await db.SaveChangesAsync();

        return Ok(result.OrderBy(r => r.RestaurantName).ToList());
    }
}
