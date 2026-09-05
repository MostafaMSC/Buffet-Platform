using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Features.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BuffetDiscovery.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(ISender mediator) : ControllerBase
{
    [HttpPost("signup")]
    public async Task<ActionResult<AuthResponseDto>> Signup(SignupCommand command, CancellationToken ct)
    {
        return Ok(await mediator.Send(command, ct));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginCommand command, CancellationToken ct)
    {
        return Ok(await mediator.Send(command, ct));
    }
}
