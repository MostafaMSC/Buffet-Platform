using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Features.Restaurants;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BuffetDiscovery.Api.Controllers;

[ApiController]
[Route("api/restaurants")]
public class RestaurantsController(ISender mediator) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<RestaurantDetailDto>> GetById(int id, [FromQuery] DateOnly? date, CancellationToken ct)
    {
        var result = await mediator.Send(new GetRestaurantDetailQuery(id, date), ct);
        return result is null ? NotFound() : Ok(result);
    }
}
