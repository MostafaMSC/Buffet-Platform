using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Features.Areas;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BuffetDiscovery.Api.Controllers;

[ApiController]
[Route("api/areas")]
public class AreasController(ISender mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<AreaDto>>> GetAreas(CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetAreasQuery(), ct));
    }
}
