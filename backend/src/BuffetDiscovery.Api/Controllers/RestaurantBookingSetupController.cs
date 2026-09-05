using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Features.Booking.Settings;
using BuffetDiscovery.Application.Features.Booking.Slots;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuffetDiscovery.Api.Controllers;

/// Restaurant-side setup for the booking system: time slots / whole-window capacity per
/// service, and the restaurant-editable booking settings (cancellation cutoff, overbooking
/// tolerance, waitlist offer window). Separate from RestaurantDashboardController (Phase 1
/// profile/service CRUD) since this is a distinct, newer bounded capability.
[ApiController]
[Route("api/dashboard/booking")]
[Authorize(Roles = "RestaurantOwner")]
public class RestaurantBookingSetupController(ISender mediator) : ControllerBase
{
    [HttpGet("services/{serviceId:int}/capacity")]
    public async Task<ActionResult<ServiceCapacityDto>> GetCapacity(int serviceId, CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetServiceCapacityQuery(serviceId), ct));
    }

    [HttpPut("services/{serviceId:int}/capacity")]
    public async Task<IActionResult> SetWholeWindowCapacity(int serviceId, UpdateServiceCapacityBody body, CancellationToken ct)
    {
        await mediator.Send(new UpdateServiceCapacityCommand(serviceId, body.Capacity), ct);
        return NoContent();
    }

    [HttpPost("slots")]
    public async Task<ActionResult<int>> CreateSlot(CreateTimeSlotCommand command, CancellationToken ct)
    {
        return Ok(await mediator.Send(command, ct));
    }

    [HttpPut("slots/{id:int}")]
    public async Task<IActionResult> UpdateSlot(int id, UpdateTimeSlotCommand command, CancellationToken ct)
    {
        await mediator.Send(command with { Id = id }, ct);
        return NoContent();
    }

    [HttpPatch("slots/{id:int}/capacity")]
    public async Task<IActionResult> UpdateSlotCapacity(int id, UpdateSlotCapacityBody body, CancellationToken ct)
    {
        await mediator.Send(new UpdateSlotCapacityCommand(id, body.Capacity), ct);
        return NoContent();
    }

    [HttpDelete("slots/{id:int}")]
    public async Task<IActionResult> DeleteSlot(int id, CancellationToken ct)
    {
        await mediator.Send(new DeleteTimeSlotCommand(id), ct);
        return NoContent();
    }

    [HttpGet("settings")]
    public async Task<ActionResult<RestaurantSettingsDto>> GetSettings(CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetRestaurantSettingsQuery(), ct));
    }

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings(UpdateRestaurantSettingsCommand command, CancellationToken ct)
    {
        await mediator.Send(command, ct);
        return NoContent();
    }
}

public record UpdateServiceCapacityBody(int? Capacity);
public record UpdateSlotCapacityBody(int Capacity);
