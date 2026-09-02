using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Features.Dashboard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuffetDiscovery.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = "RestaurantOwner")]
public class RestaurantDashboardController(ISender mediator) : ControllerBase
{
    [HttpGet("profile")]
    public async Task<ActionResult<RestaurantProfileDto>> GetProfile(CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetOwnerProfileQuery(), ct));
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(UpdateProfileCommand command, CancellationToken ct)
    {
        await mediator.Send(command, ct);
        return NoContent();
    }

    [HttpGet("offerings")]
    public async Task<ActionResult<List<DashboardOfferingDto>>> GetOfferings([FromQuery] int days = 14, CancellationToken ct = default)
    {
        return Ok(await mediator.Send(new GetDashboardOfferingsQuery(days), ct));
    }

    [HttpPost("offerings")]
    public async Task<ActionResult<int>> CreateOffering(CreateOfferingCommand command, CancellationToken ct)
    {
        return Ok(await mediator.Send(command, ct));
    }

    [HttpPut("offerings/{id:int}")]
    public async Task<IActionResult> UpdateOffering(int id, UpdateOfferingCommand command, CancellationToken ct)
    {
        await mediator.Send(command with { Id = id }, ct);
        return NoContent();
    }

    [HttpDelete("offerings/{id:int}")]
    public async Task<IActionResult> DeleteOffering(int id, CancellationToken ct)
    {
        await mediator.Send(new DeleteOfferingCommand(id), ct);
        return NoContent();
    }

    [HttpPost("availability/toggle")]
    public async Task<IActionResult> Toggle(ToggleAvailabilityCommand command, CancellationToken ct)
    {
        await mediator.Send(command, ct);
        return NoContent();
    }
}
