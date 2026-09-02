using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuffetDiscovery.Infrastructure.Persistence.Repositories;

public class AreaRepository(AppDbContext db) : IAreaRepository
{
    public Task<List<Area>> GetAllAsync(CancellationToken ct) =>
        db.Areas.OrderBy(a => a.SortOrder).ToListAsync(ct);

    public Task<Area?> GetByIdAsync(int id, CancellationToken ct) =>
        db.Areas.FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<bool> ExistsAsync(int id, CancellationToken ct) =>
        db.Areas.AnyAsync(a => a.Id == id, ct);
}
