using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Features.Admin;
using BuffetDiscovery.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuffetDiscovery.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController(ISender mediator) : ControllerBase
{
    [HttpGet("restaurants")]
    public async Task<ActionResult<List<RestaurantAdminListItemDto>>> GetRestaurants([FromQuery] RestaurantStatus? status, CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetAdminRestaurantsQuery(status), ct));
    }

    [HttpPost("restaurants/{id:int}/approve")]
    public async Task<IActionResult> Approve(int id, CancellationToken ct)
    {
        await mediator.Send(new ApproveRestaurantCommand(id), ct);
        return NoContent();
    }

    [HttpPost("restaurants/{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, CancellationToken ct)
    {
        await mediator.Send(new RejectRestaurantCommand(id), ct);
        return NoContent();
    }

    [HttpPost("restaurants/{id:int}/suspend")]
    public async Task<IActionResult> Suspend(int id, CancellationToken ct)
    {
        await mediator.Send(new SuspendRestaurantCommand(id), ct);
        return NoContent();
    }

    [HttpPost("restaurants/{id:int}/reinstate")]
    public async Task<IActionResult> Reinstate(int id, CancellationToken ct)
    {
        await mediator.Send(new ReinstateRestaurantCommand(id), ct);
        return NoContent();
    }

    [HttpPut("restaurants/{id:int}")]
    public async Task<IActionResult> UpdateRestaurant(int id, AdminUpdateRestaurantCommand command, CancellationToken ct)
    {
        await mediator.Send(command with { Id = id }, ct);
        return NoContent();
    }

    [HttpDelete("offerings/{id:int}")]
    public async Task<IActionResult> DeleteOffering(int id, CancellationToken ct)
    {
        await mediator.Send(new AdminDeleteOfferingCommand(id), ct);
        return NoContent();
    }

    [HttpGet("booking-settings")]
    public async Task<ActionResult<List<AdminRestaurantSettingsDto>>> GetBookingSettings(CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetAdminBookingSettingsQuery(), ct));
    }

    [HttpPut("booking-settings/{restaurantId:int}")]
    public async Task<IActionResult> UpdateBookingSettings(int restaurantId, UpdateAdminRestaurantSettingsCommand command, CancellationToken ct)
    {
        await mediator.Send(command with { RestaurantId = restaurantId }, ct);
        return NoContent();
    }

    [HttpGet("bookings/stats")]
    public async Task<ActionResult<PlatformBookingStatsDto>> GetBookingStats([FromQuery] DateOnly start, [FromQuery] DateOnly end, CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetPlatformBookingStatsQuery(start, end), ct));
    }
}
