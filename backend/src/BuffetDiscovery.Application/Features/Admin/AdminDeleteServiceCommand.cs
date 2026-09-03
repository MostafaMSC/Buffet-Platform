using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using MediatR;

namespace BuffetDiscovery.Application.Features.Admin;

public record AdminDeleteServiceCommand(int Id) : IRequest;

public class AdminDeleteServiceCommandHandler(IServiceRepository services, IUnitOfWork unitOfWork)
    : IRequestHandler<AdminDeleteServiceCommand>
{
    public async Task Handle(AdminDeleteServiceCommand request, CancellationToken ct)
    {
        var service = await services.GetByIdAsync(request.Id, ct) ?? throw new NotFoundException("Service not found.");
        service.IsDeleted = true;
        await unitOfWork.SaveChangesAsync(ct);
    }
}
