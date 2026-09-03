using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Features.Booking.Restaurant;
using BuffetDiscovery.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuffetDiscovery.Api.Controllers;

/// Day-to-day booking operations: the overview tiles, the booking list, status actions and
/// analytics. Separate from RestaurantDashboardController, which is about configuring
/// services rather than running service.
[ApiController]
[Route("api/dashboard/bookings")]
[Authorize(Roles = "RestaurantOwner")]
public class RestaurantBookingDashboardController(ISender mediator) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<ActionResult<DashboardOverviewDto>> Overview(CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetDashboardOverviewQuery(), ct));
    }

    [HttpGet]
    public async Task<ActionResult<List<RestaurantBookingGroupDto>>> GetBookings(
        [FromQuery] DateOnly? date, [FromQuery] BookingStatus? status, CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetRestaurantBookingsQuery(date, status), ct));
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> MarkStatus(int id, MarkBookingStatusBody body, CancellationToken ct)
    {
        await mediator.Send(new MarkBookingStatusCommand(id, body.Status), ct);
        return NoContent();
    }

    [HttpGet("analytics")]
    public async Task<ActionResult<BookingAnalyticsDto>> GetAnalytics(
        [FromQuery] DateOnly start, [FromQuery] DateOnly end, CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetBookingAnalyticsQuery(start, end), ct));
    }
}

public record MarkBookingStatusBody(BookingStatus Status);
