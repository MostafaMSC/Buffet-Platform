using BuffetDiscovery.Domain.Entities;

namespace BuffetDiscovery.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string CreateToken(User user);
}
