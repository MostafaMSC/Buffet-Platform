using BuffetDiscovery.Api.Data;
using BuffetDiscovery.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BuffetDiscovery.Api.Controllers;

[ApiController]
[Route("api/areas")]
public class AreasController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<AreaDto>>> GetAreas()
    {
        var areas = await db.Areas
            .OrderBy(a => a.SortOrder)
            .Select(a => new AreaDto(a.Id, a.NameEn, a.NameAr))
            .ToListAsync();

        return Ok(areas);
    }
}
