using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Features.Dashboard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuffetDiscovery.Api.Controllers;

/// The restaurant's own management surface: profile, services (with menus and sittings),
/// day-by-day availability and the calendar.
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

    [HttpGet("services")]
    public async Task<ActionResult<List<DashboardServiceDto>>> GetServices([FromQuery] int days = 14, CancellationToken ct = default)
    {
        return Ok(await mediator.Send(new GetDashboardServicesQuery(days), ct));
    }

    [HttpGet("services/{id:int}")]
    public async Task<ActionResult<ServiceEditorDto>> GetService(int id, CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetServiceEditorQuery(id), ct));
    }

    [HttpPost("services")]
    public async Task<ActionResult<int>> CreateService(ServiceInput service, CancellationToken ct)
    {
        return Ok(await mediator.Send(new CreateServiceCommand(service), ct));
    }

    [HttpPut("services/{id:int}")]
    public async Task<IActionResult> UpdateService(int id, ServiceInput service, CancellationToken ct)
    {
        await mediator.Send(new UpdateServiceCommand(id, service), ct);
        return NoContent();
    }

    [HttpDelete("services/{id:int}")]
    public async Task<IActionResult> DeleteService(int id, CancellationToken ct)
    {
        await mediator.Send(new DeleteServiceCommand(id), ct);
        return NoContent();
    }

    [HttpPost("availability/toggle")]
    public async Task<IActionResult> ToggleAvailability(ToggleAvailabilityCommand command, CancellationToken ct)
    {
        await mediator.Send(command, ct);
        return NoContent();
    }

    [HttpGet("calendar")]
    public async Task<ActionResult<List<CalendarDayDto>>> GetCalendar(
        [FromQuery] DateOnly from, [FromQuery] DateOnly to, [FromQuery] int? serviceId, CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetCalendarQuery(from, to, serviceId), ct));
    }

    [HttpPut("calendar/slot-override")]
    public async Task<IActionResult> SetSlotOverride(SetSlotOverrideCommand command, CancellationToken ct)
    {
        await mediator.Send(command, ct);
        return NoContent();
    }
}
