using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace BuffetDiscovery.Application.Features.Booking.Slots;

/// The fast, one-field capacity edit the brief calls for ("not a buried setting") — as
/// restaurants learn their own turnover patterns they can bump a slot's capacity up or
/// down without opening the full slot editor.
public record UpdateSlotCapacityCommand(int Id, int Capacity) : IRequest;

public class UpdateSlotCapacityCommandValidator : AbstractValidator<UpdateSlotCapacityCommand>
{
    public UpdateSlotCapacityCommandValidator()
    {
        RuleFor(x => x.Capacity).GreaterThan(0);
    }
}

public class UpdateSlotCapacityCommandHandler(
    ITimeSlotRepository timeSlots,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateSlotCapacityCommand>
{
    public async Task Handle(UpdateSlotCapacityCommand request, CancellationToken ct)
    {
        var restaurantId = currentUser.RestaurantId ?? throw new UnauthorizedException("No restaurant associated with this account.");
        var slot = await timeSlots.GetByIdForRestaurantAsync(request.Id, restaurantId, ct)
            ?? throw new NotFoundException("Time slot not found.");

        slot.Capacity = request.Capacity;
        await unitOfWork.SaveChangesAsync(ct);
    }
}
