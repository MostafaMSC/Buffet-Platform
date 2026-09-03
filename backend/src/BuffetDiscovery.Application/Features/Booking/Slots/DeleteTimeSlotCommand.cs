using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using MediatR;

namespace BuffetDiscovery.Application.Features.Booking.Slots;

public record DeleteTimeSlotCommand(int Id) : IRequest;

public class DeleteTimeSlotCommandHandler(
    ITimeSlotRepository timeSlots,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteTimeSlotCommand>
{
    public async Task Handle(DeleteTimeSlotCommand request, CancellationToken ct)
    {
        var restaurantId = currentUser.RestaurantId ?? throw new UnauthorizedException("No restaurant associated with this account.");
        var slot = await timeSlots.GetByIdForRestaurantAsync(request.Id, restaurantId, ct)
            ?? throw new NotFoundException("Time slot not found.");

        // Soft delete: existing Bookings keep referencing this TimeSlotId untouched.
        slot.IsDeleted = true;
        await unitOfWork.SaveChangesAsync(ct);
    }
}
