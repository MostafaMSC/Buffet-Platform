using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Entities;
using MediatR;

namespace BuffetDiscovery.Application.Features.Dashboard;

public record ToggleAvailabilityCommand(int ServiceId, DateOnly Date, bool IsActive) : IRequest;

public class ToggleAvailabilityCommandHandler(
    IServiceRepository services,
    IAvailabilityRepository availability,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<ToggleAvailabilityCommand>
{
    public async Task Handle(ToggleAvailabilityCommand request, CancellationToken ct)
    {
        var restaurantId = currentUser.RestaurantId ?? throw new UnauthorizedException("No restaurant associated with this account.");

        // Ownership check: the service must belong to the caller's restaurant.
        _ = await services.GetByIdForRestaurantAsync(request.ServiceId, restaurantId, ct)
            ?? throw new NotFoundException("Service not found.");

        var status = await availability.GetAsync(request.ServiceId, request.Date, ct);
        if (status is null)
        {
            availability.Add(new AvailabilityStatus { ServiceId = request.ServiceId, Date = request.Date, IsActive = request.IsActive });
        }
        else
        {
            status.IsActive = request.IsActive;
        }

        await unitOfWork.SaveChangesAsync(ct);
    }
}
