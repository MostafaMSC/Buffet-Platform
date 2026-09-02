using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using MediatR;

namespace BuffetDiscovery.Application.Features.Dashboard;

public record DeleteOfferingCommand(int Id) : IRequest;

public class DeleteOfferingCommandHandler(
    IOfferingRepository offerings,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteOfferingCommand>
{
    public async Task Handle(DeleteOfferingCommand request, CancellationToken ct)
    {
        var restaurantId = currentUser.RestaurantId ?? throw new UnauthorizedException("No restaurant associated with this account.");
        var offering = await offerings.GetByIdForRestaurantAsync(request.Id, restaurantId, ct)
            ?? throw new NotFoundException("Offering not found.");

        offering.IsDeleted = true;
        await unitOfWork.SaveChangesAsync(ct);
    }
}
