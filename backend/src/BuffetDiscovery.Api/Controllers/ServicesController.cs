using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Features.Booking.Customer;
using BuffetDiscovery.Application.Features.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BuffetDiscovery.Api.Controllers;

/// The public service (buffet / set menu) surface: full detail for the booking page, and a
/// lighter availability call the booking widget re-reads as the customer changes date or
/// party size.
[ApiController]
[Route("api/services")]
public class ServicesController(ISender mediator) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ServiceDetailDto>> Detail(
        int id,
        [FromQuery] DateOnly? date,
        [FromQuery] int adults = 2,
        [FromQuery] int children = 0,
        CancellationToken ct = default)
    {
        return Ok(await mediator.Send(new GetServiceDetailQuery(id, date, adults, children), ct));
    }

    [HttpGet("{id:int}/availability")]
    public async Task<ActionResult<ServiceAvailabilityDto>> Availability(
        int id,
        [FromQuery] DateOnly date,
        [FromQuery] int guests = 1,
        CancellationToken ct = default)
    {
        return Ok(await mediator.Send(new GetBookingAvailabilityQuery(id, date, guests), ct));
    }
}
