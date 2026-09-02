using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Common.Interfaces;
using MediatR;

namespace BuffetDiscovery.Application.Features.Areas;

public record GetAreasQuery : IRequest<List<AreaDto>>;

public class GetAreasQueryHandler(IAreaRepository areas) : IRequestHandler<GetAreasQuery, List<AreaDto>>
{
    public async Task<List<AreaDto>> Handle(GetAreasQuery request, CancellationToken ct)
    {
        var all = await areas.GetAllAsync(ct);
        return all.Select(a => new AreaDto(a.Id, a.NameEn, a.NameAr)).ToList();
    }
}
