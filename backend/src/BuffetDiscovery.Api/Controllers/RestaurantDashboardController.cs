using System.Security.Claims;
using BuffetDiscovery.Api.Data;
using BuffetDiscovery.Api.Dtos;
using BuffetDiscovery.Api.Entities;
using BuffetDiscovery.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BuffetDiscovery.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = "RestaurantOwner")]
public class RestaurantDashboardController(AppDbContext db, AvailabilityService availability) : ControllerBase
{
    private int? CurrentRestaurantId =>
        int.TryParse(User.FindFirstValue("restaurantId"), out var id) ? id : null;

    [HttpGet("profile")]
    public async Task<ActionResult<RestaurantAdminListItemDto>> GetProfile()
    {
        var restaurantId = CurrentRestaurantId;
        if (restaurantId is null) return Forbid();

        var r = await db.Restaurants.Include(x => x.Area)
            .FirstOrDefaultAsync(x => x.Id == restaurantId);
        if (r is null) return NotFound();

        return Ok(new
        {
            r.Id,
            r.Name,
            r.NameAr,
            r.AreaId,
            AreaNameEn = r.Area!.NameEn,
            r.PhoneNumber,
            r.Address,
            r.GoogleMapsUrl,
            r.Description,
            r.DescriptionAr,
            r.LogoUrl,
            r.CoverPhotoUrl,
            r.Status
        });
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(RestaurantProfileInputDto dto)
    {
        var restaurantId = CurrentRestaurantId;
        if (restaurantId is null) return Forbid();

        var r = await db.Restaurants.FirstOrDefaultAsync(x => x.Id == restaurantId);
        if (r is null) return NotFound();

        var areaExists = await db.Areas.AnyAsync(a => a.Id == dto.AreaId);
        if (!areaExists) return BadRequest(new { message = "Invalid area." });

        r.Name = dto.Name;
        r.NameAr = dto.NameAr;
        r.AreaId = dto.AreaId;
        r.PhoneNumber = dto.PhoneNumber;
        r.Address = dto.Address;
        r.GoogleMapsUrl = dto.GoogleMapsUrl;
        r.Description = dto.Description;
        r.DescriptionAr = dto.DescriptionAr;
        r.LogoUrl = dto.LogoUrl;
        r.CoverPhotoUrl = dto.CoverPhotoUrl;

        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("offerings")]
    public async Task<ActionResult<List<DashboardOfferingDto>>> GetOfferings([FromQuery] int days = 14)
    {
        var restaurantId = CurrentRestaurantId;
        if (restaurantId is null) return Forbid();

        var offerings = await db.Offerings
            .Include(o => o.Photos)
            .Where(o => o.RestaurantId == restaurantId && !o.IsDeleted)
            .ToListAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3));
        var endDate = today.AddDays(days - 1);

        foreach (var offering in offerings)
        {
            await availability.MaterializeRangeAsync(offering, today, endDate);
        }
        await db.SaveChangesAsync();

        var offeringIds = offerings.Select(o => o.Id).ToList();
        var statuses = await db.AvailabilityStatuses
            .Where(a => offeringIds.Contains(a.OfferingId) && a.Date >= today && a.Date <= endDate)
            .ToListAsync();

        var result = offerings.Select(o => new DashboardOfferingDto(
            o.Id,
            o.MealType,
            o.Price,
            o.OpensAt.ToString("HH:mm"),
            o.ClosesAt.ToString("HH:mm"),
            o.Description,
            o.DescriptionAr,
            o.Recurrence,
            WeekdaysToList(o.Weekdays),
            o.RamadanStartDate,
            o.RamadanEndDate,
            o.OneOffDate,
            o.Photos.OrderBy(p => p.SortOrder).Select(p => p.Url).ToList(),
            statuses.Where(s => s.OfferingId == o.Id)
                .OrderBy(s => s.Date)
                .Select(s => new DayStatusDto(s.Date, s.IsActive))
                .ToList()
        )).ToList();

        return Ok(result);
    }

    [HttpPost("offerings")]
    public async Task<ActionResult<int>> CreateOffering(OfferingInputDto dto)
    {
        var restaurantId = CurrentRestaurantId;
        if (restaurantId is null) return Forbid();

        if (!TimeOnly.TryParse(dto.OpensAt, out var opensAt) || !TimeOnly.TryParse(dto.ClosesAt, out var closesAt))
        {
            return BadRequest(new { message = "Invalid time format, expected HH:mm." });
        }

        var offering = new BuffetOffering
        {
            RestaurantId = restaurantId.Value,
            MealType = dto.MealType,
            Price = dto.Price,
            OpensAt = opensAt,
            ClosesAt = closesAt,
            Description = dto.Description,
            DescriptionAr = dto.DescriptionAr,
            Recurrence = dto.Recurrence,
            Weekdays = ListToWeekdays(dto.Weekdays),
            RamadanStartDate = dto.RamadanStartDate,
            RamadanEndDate = dto.RamadanEndDate,
            OneOffDate = dto.OneOffDate
        };

        if (dto.PhotoUrls is not null)
        {
            offering.Photos = dto.PhotoUrls.Select((url, i) => new OfferingPhoto { Url = url, SortOrder = i }).ToList();
        }

        db.Offerings.Add(offering);
        await db.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3));
        await availability.MaterializeRangeAsync(offering, today, today.AddDays(13));
        await db.SaveChangesAsync();

        return Ok(offering.Id);
    }

    [HttpPut("offerings/{id:int}")]
    public async Task<IActionResult> UpdateOffering(int id, OfferingInputDto dto)
    {
        var restaurantId = CurrentRestaurantId;
        if (restaurantId is null) return Forbid();

        var offering = await db.Offerings.Include(o => o.Photos)
            .FirstOrDefaultAsync(o => o.Id == id && o.RestaurantId == restaurantId);
        if (offering is null) return NotFound();

        if (!TimeOnly.TryParse(dto.OpensAt, out var opensAt) || !TimeOnly.TryParse(dto.ClosesAt, out var closesAt))
        {
            return BadRequest(new { message = "Invalid time format, expected HH:mm." });
        }

        offering.MealType = dto.MealType;
        offering.Price = dto.Price;
        offering.OpensAt = opensAt;
        offering.ClosesAt = closesAt;
        offering.Description = dto.Description;
        offering.DescriptionAr = dto.DescriptionAr;
        offering.Recurrence = dto.Recurrence;
        offering.Weekdays = ListToWeekdays(dto.Weekdays);
        offering.RamadanStartDate = dto.RamadanStartDate;
        offering.RamadanEndDate = dto.RamadanEndDate;
        offering.OneOffDate = dto.OneOffDate;

        if (dto.PhotoUrls is not null)
        {
            db.OfferingPhotos.RemoveRange(offering.Photos);
            offering.Photos = dto.PhotoUrls.Select((url, i) => new OfferingPhoto { Url = url, SortOrder = i }).ToList();
        }

        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("offerings/{id:int}")]
    public async Task<IActionResult> DeleteOffering(int id)
    {
        var restaurantId = CurrentRestaurantId;
        if (restaurantId is null) return Forbid();

        var offering = await db.Offerings.FirstOrDefaultAsync(o => o.Id == id && o.RestaurantId == restaurantId);
        if (offering is null) return NotFound();

        offering.IsDeleted = true;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("availability/toggle")]
    public async Task<IActionResult> Toggle(ToggleAvailabilityDto dto)
    {
        var restaurantId = CurrentRestaurantId;
        if (restaurantId is null) return Forbid();

        var offering = await db.Offerings
            .FirstOrDefaultAsync(o => o.Id == dto.OfferingId && o.RestaurantId == restaurantId);
        if (offering is null) return NotFound();

        var status = await db.AvailabilityStatuses
            .FirstOrDefaultAsync(a => a.OfferingId == dto.OfferingId && a.Date == dto.Date);

        if (status is null)
        {
            status = new AvailabilityStatus { OfferingId = dto.OfferingId, Date = dto.Date, IsActive = dto.IsActive };
            db.AvailabilityStatuses.Add(status);
        }
        else
        {
            status.IsActive = dto.IsActive;
        }

        await db.SaveChangesAsync();
        return NoContent();
    }

    private static List<string> WeekdaysToList(WeekDays w) =>
        Enum.GetValues<WeekDays>()
            .Where(v => v != WeekDays.None && w.HasFlag(v))
            .Select(v => v.ToString())
            .ToList();

    private static WeekDays ListToWeekdays(List<string>? days)
    {
        if (days is null) return WeekDays.None;
        var result = WeekDays.None;
        foreach (var d in days)
        {
            if (Enum.TryParse<WeekDays>(d, true, out var parsed))
            {
                result |= parsed;
            }
        }
        return result;
    }
}
