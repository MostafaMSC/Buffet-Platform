using BuffetDiscovery.Api.Data;
using BuffetDiscovery.Api.Dtos;
using BuffetDiscovery.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BuffetDiscovery.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController(AppDbContext db) : ControllerBase
{
    [HttpGet("restaurants")]
    public async Task<ActionResult<List<RestaurantAdminListItemDto>>> GetRestaurants([FromQuery] RestaurantStatus? status)
    {
        var query = db.Restaurants.Include(r => r.Area).Include(r => r.Offerings).AsQueryable();
        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        var restaurants = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();

        var result = restaurants.Select(r => new RestaurantAdminListItemDto(
            r.Id, r.Name, r.NameAr, r.Area!.NameEn, r.PhoneNumber, r.Status, r.CreatedAt,
            r.Offerings.Count(o => !o.IsDeleted)
        )).ToList();

        return Ok(result);
    }

    [HttpPost("restaurants/{id:int}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        var r = await db.Restaurants.FindAsync(id);
        if (r is null) return NotFound();
        r.Status = RestaurantStatus.Approved;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("restaurants/{id:int}/reject")]
    public async Task<IActionResult> Reject(int id)
    {
        var r = await db.Restaurants.FindAsync(id);
        if (r is null) return NotFound();
        r.Status = RestaurantStatus.Rejected;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("restaurants/{id:int}/suspend")]
    public async Task<IActionResult> Suspend(int id)
    {
        var r = await db.Restaurants.FindAsync(id);
        if (r is null) return NotFound();
        r.Status = RestaurantStatus.Suspended;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("restaurants/{id:int}/reinstate")]
    public async Task<IActionResult> Reinstate(int id)
    {
        var r = await db.Restaurants.FindAsync(id);
        if (r is null) return NotFound();
        r.Status = RestaurantStatus.Approved;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("restaurants/{id:int}")]
    public async Task<IActionResult> UpdateRestaurant(int id, RestaurantProfileInputDto dto)
    {
        var r = await db.Restaurants.FindAsync(id);
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

    [HttpDelete("offerings/{id:int}")]
    public async Task<IActionResult> DeleteOffering(int id)
    {
        var offering = await db.Offerings.FindAsync(id);
        if (offering is null) return NotFound();
        offering.IsDeleted = true;
        await db.SaveChangesAsync();
        return NoContent();
    }
}
