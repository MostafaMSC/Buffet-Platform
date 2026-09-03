using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Features.Booking.Customer;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BuffetDiscovery.Api.Controllers;

/// The public, account-free customer booking surface: check availability, book, join a
/// waitlist, confirm a waitlist offer, and look bookings up — by confirmation code (the
/// "badge") or by phone number. No [Authorize] anywhere in this controller by design.
[ApiController]
[Route("api/bookings")]
public class BookingController(ISender mediator) : ControllerBase
{
    [HttpGet("availability")]
    public async Task<ActionResult<BookingAvailabilityDto>> GetAvailability(
        [FromQuery] int serviceId, [FromQuery] DateOnly date, CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetBookingAvailabilityQuery(serviceId, date), ct));
    }

    [HttpPost]
    public async Task<ActionResult<BookingDetailDto>> Create(CreateBookingCommand command, CancellationToken ct)
    {
        return Ok(await mediator.Send(command, ct));
    }

    [HttpPost("waitlist")]
    public async Task<ActionResult<WaitlistDetailDto>> JoinWaitlist(JoinWaitlistCommand command, CancellationToken ct)
    {
        return Ok(await mediator.Send(command, ct));
    }

    [HttpPost("waitlist/{waitlistId:int}/confirm")]
    public async Task<ActionResult<BookingDetailDto>> ConfirmWaitlistOffer(
        int waitlistId, ConfirmWaitlistOfferBody body, CancellationToken ct)
    {
        return Ok(await mediator.Send(new ConfirmWaitlistOfferCommand(waitlistId, body.CustomerPhone), ct));
    }

    [HttpGet("{confirmationCode}")]
    public async Task<ActionResult<BookingDetailDto>> GetByCode(string confirmationCode, CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetBookingByConfirmationCodeQuery(confirmationCode), ct));
    }

    [HttpPost("{confirmationCode}/cancel")]
    public async Task<IActionResult> Cancel(string confirmationCode, CancellationToken ct)
    {
        await mediator.Send(new CancelBookingCommand(confirmationCode), ct);
        return NoContent();
    }

    [HttpGet("mine")]
    public async Task<ActionResult<MyLookupResultDto>> GetMine([FromQuery] string phone, CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetMyBookingsQuery(phone), ct));
    }
}

public record ConfirmWaitlistOfferBody(string CustomerPhone);
