using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace BuffetDiscovery.Application.Features.Booking.Slots;

/// Sets (or clears) the whole-window booking capacity for an service that isn't split
/// into time slots. Rejected if the service currently has active slots — an service is
/// in exactly one mode at a time (whole-window OR slots), never both, to keep the booking
/// logic and the restaurant's mental model simple.
public record UpdateServiceCapacityCommand(int ServiceId, int? Capacity) : IRequest;

public class UpdateServiceCapacityCommandValidator : AbstractValidator<UpdateServiceCapacityCommand>
{
    public UpdateServiceCapacityCommandValidator()
    {
        RuleFor(x => x.Capacity).GreaterThan(0).When(x => x.Capacity.HasValue);
    }
}

public class UpdateServiceCapacityCommandHandler(
    IServiceRepository services,
    ITimeSlotRepository timeSlots,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateServiceCapacityCommand>
{
    public async Task Handle(UpdateServiceCapacityCommand request, CancellationToken ct)
    {
        var restaurantId = currentUser.RestaurantId ?? throw new UnauthorizedException("No restaurant associated with this account.");
        var service = await services.GetByIdForRestaurantAsync(request.ServiceId, restaurantId, ct)
            ?? throw new NotFoundException("Service not found.");

        if (request.Capacity.HasValue)
        {
            var slots = await timeSlots.GetByServiceAsync(service.Id, ct);
            if (slots.Count > 0)
            {
                throw new ConflictException("This service is split into time slots. Remove them first to set a single whole-window capacity.");
            }
        }

        service.Capacity = request.Capacity;
        await unitOfWork.SaveChangesAsync(ct);
    }
}
