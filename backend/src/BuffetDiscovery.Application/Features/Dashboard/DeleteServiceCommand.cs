using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using MediatR;

namespace BuffetDiscovery.Application.Features.Dashboard;

public record DeleteServiceCommand(int Id) : IRequest;

public class DeleteServiceCommandHandler(
    IServiceRepository services,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteServiceCommand>
{
    public async Task Handle(DeleteServiceCommand request, CancellationToken ct)
    {
        var restaurantId = currentUser.RestaurantId ?? throw new UnauthorizedException("No restaurant associated with this account.");
        var service = await services.GetByIdForRestaurantAsync(request.Id, restaurantId, ct)
            ?? throw new NotFoundException("Service not found.");

        service.IsDeleted = true;
        await unitOfWork.SaveChangesAsync(ct);
    }
}
