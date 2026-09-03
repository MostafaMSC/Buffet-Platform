using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace BuffetDiscovery.Application.Features.Booking.Slots;

/// Sets (or clears) the whole-window booking capacity for an offering that isn't split
/// into time slots. Rejected if the offering currently has active slots — an offering is
/// in exactly one mode at a time (whole-window OR slots), never both, to keep the booking
/// logic and the restaurant's mental model simple.
public record UpdateOfferingCapacityCommand(int OfferingId, int? Capacity) : IRequest;

public class UpdateOfferingCapacityCommandValidator : AbstractValidator<UpdateOfferingCapacityCommand>
{
    public UpdateOfferingCapacityCommandValidator()
    {
        RuleFor(x => x.Capacity).GreaterThan(0).When(x => x.Capacity.HasValue);
    }
}

public class UpdateOfferingCapacityCommandHandler(
    IOfferingRepository offerings,
    ITimeSlotRepository timeSlots,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateOfferingCapacityCommand>
{
    public async Task Handle(UpdateOfferingCapacityCommand request, CancellationToken ct)
    {
        var restaurantId = currentUser.RestaurantId ?? throw new UnauthorizedException("No restaurant associated with this account.");
        var offering = await offerings.GetByIdForRestaurantAsync(request.OfferingId, restaurantId, ct)
            ?? throw new NotFoundException("Offering not found.");

        if (request.Capacity.HasValue)
        {
            var slots = await timeSlots.GetByOfferingAsync(offering.Id, ct);
            if (slots.Count > 0)
            {
                throw new ConflictException("This offering is split into time slots. Remove them first to set a single whole-window capacity.");
            }
        }

        offering.Capacity = request.Capacity;
        await unitOfWork.SaveChangesAsync(ct);
    }
}
