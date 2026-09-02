using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using MediatR;

namespace BuffetDiscovery.Application.Features.Admin;

public record AdminDeleteOfferingCommand(int Id) : IRequest;

public class AdminDeleteOfferingCommandHandler(IOfferingRepository offerings, IUnitOfWork unitOfWork)
    : IRequestHandler<AdminDeleteOfferingCommand>
{
    public async Task Handle(AdminDeleteOfferingCommand request, CancellationToken ct)
    {
        var offering = await offerings.GetByIdAsync(request.Id, ct) ?? throw new NotFoundException("Offering not found.");
        offering.IsDeleted = true;
        await unitOfWork.SaveChangesAsync(ct);
    }
}
