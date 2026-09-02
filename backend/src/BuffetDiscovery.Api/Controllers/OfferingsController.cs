using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Features.Offerings;
using BuffetDiscovery.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BuffetDiscovery.Api.Controllers;

[ApiController]
[Route("api/offerings")]
public class OfferingsController(ISender mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<OfferingListItemDto>>> Browse(
        [FromQuery] DateOnly? date,
        [FromQuery] int? areaId,
        [FromQuery] MealType? mealType,
        CancellationToken ct)
    {
        return Ok(await mediator.Send(new BrowseOfferingsQuery(date, areaId, mealType), ct));
    }
}
