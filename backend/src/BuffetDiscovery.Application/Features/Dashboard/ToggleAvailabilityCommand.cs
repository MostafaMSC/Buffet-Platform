using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Entities;
using MediatR;

namespace BuffetDiscovery.Application.Features.Dashboard;

public record ToggleAvailabilityCommand(int OfferingId, DateOnly Date, bool IsActive) : IRequest;

public class ToggleAvailabilityCommandHandler(
    IOfferingRepository offerings,
    IAvailabilityRepository availability,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<ToggleAvailabilityCommand>
{
    public async Task Handle(ToggleAvailabilityCommand request, CancellationToken ct)
    {
        var restaurantId = currentUser.RestaurantId ?? throw new UnauthorizedException("No restaurant associated with this account.");

        // Ownership check: the offering must belong to the caller's restaurant.
        _ = await offerings.GetByIdForRestaurantAsync(request.OfferingId, restaurantId, ct)
            ?? throw new NotFoundException("Offering not found.");

        var status = await availability.GetAsync(request.OfferingId, request.Date, ct);
        if (status is null)
        {
            availability.Add(new AvailabilityStatus { OfferingId = request.OfferingId, Date = request.Date, IsActive = request.IsActive });
        }
        else
        {
            status.IsActive = request.IsActive;
        }

        await unitOfWork.SaveChangesAsync(ct);
    }
}
