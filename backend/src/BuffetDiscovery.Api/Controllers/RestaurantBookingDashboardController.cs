using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Features.Booking.Restaurant;
using BuffetDiscovery.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuffetDiscovery.Api.Controllers;

/// The restaurant's day-to-day booking dashboard: see who's booked, mark no-shows/completed,
/// and view booking analytics. Distinct from RestaurantBookingSetupController (capacity/slot
/// configuration, done once) — this is the recurring, day-of-service workflow.
[ApiController]
[Route("api/dashboard/bookings")]
[Authorize(Roles = "RestaurantOwner")]
public class RestaurantBookingDashboardController(ISender mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<RestaurantBookingGroupDto>>> GetBookings([FromQuery] DateOnly? date, CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetRestaurantBookingsQuery(date), ct));
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> MarkStatus(int id, MarkBookingStatusBody body, CancellationToken ct)
    {
        await mediator.Send(new MarkBookingStatusCommand(id, body.Status), ct);
        return NoContent();
    }

    [HttpGet("analytics")]
    public async Task<ActionResult<BookingAnalyticsDto>> GetAnalytics([FromQuery] DateOnly start, [FromQuery] DateOnly end, CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetBookingAnalyticsQuery(start, end), ct));
    }
}

public record MarkBookingStatusBody(BookingStatus Status);
