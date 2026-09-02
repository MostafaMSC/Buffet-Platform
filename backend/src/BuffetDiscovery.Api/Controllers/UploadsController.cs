using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Features.Uploads;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuffetDiscovery.Api.Controllers;

[ApiController]
[Route("api/uploads")]
[Authorize(Roles = "RestaurantOwner,Admin")]
public class UploadsController(ISender mediator) : ControllerBase
{
    private const long MaxFileSizeBytes = 8 * 1024 * 1024;

    [HttpPost]
    [RequestSizeLimit(MaxFileSizeBytes)]
    public async Task<ActionResult<UploadResultDto>> Upload(IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        var result = await mediator.Send(new UploadFileCommand(stream, file.FileName, file.Length), ct);
        return Ok(result);
    }
}
