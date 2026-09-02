using BuffetDiscovery.Domain.Entities;

namespace BuffetDiscovery.Application.Common.Interfaces;

public interface IAreaRepository
{
    Task<List<Area>> GetAllAsync(CancellationToken ct);
    Task<Area?> GetByIdAsync(int id, CancellationToken ct);
    Task<bool> ExistsAsync(int id, CancellationToken ct);
}
