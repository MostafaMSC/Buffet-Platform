using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace BuffetDiscovery.Application.Features.Booking.Slots;

/// Capacity/time edits apply going forward only — existing Bookings already reference this
/// TimeSlotId and are untouched; only future capacity checks and the dashboard display use
/// the new values. This is meant to be a fast, low-friction edit (see UpdateSlotCapacityCommand
/// below for the even-faster capacity-only path used by the dashboard's inline editor).
public record UpdateTimeSlotCommand(int Id, string StartTime, string EndTime, int Capacity, int BufferMinutes) : IRequest;

public class UpdateTimeSlotCommandValidator : AbstractValidator<UpdateTimeSlotCommand>
{
    public UpdateTimeSlotCommandValidator()
    {
        RuleFor(x => x.StartTime).Must(t => TimeOnly.TryParse(t, out _)).WithMessage("Invalid time format, expected HH:mm.");
        RuleFor(x => x.EndTime).Must(t => TimeOnly.TryParse(t, out _)).WithMessage("Invalid time format, expected HH:mm.");
        RuleFor(x => x.Capacity).GreaterThan(0);
        RuleFor(x => x.BufferMinutes).GreaterThanOrEqualTo(0);
        RuleFor(x => x).Must(x => !TimeOnly.TryParse(x.StartTime, out var s) || !TimeOnly.TryParse(x.EndTime, out var e) || s < e)
            .WithMessage("Start time must be before end time.");
    }
}

public class UpdateTimeSlotCommandHandler(
    ITimeSlotRepository timeSlots,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateTimeSlotCommand>
{
    public async Task Handle(UpdateTimeSlotCommand request, CancellationToken ct)
    {
        var restaurantId = currentUser.RestaurantId ?? throw new UnauthorizedException("No restaurant associated with this account.");
        var slot = await timeSlots.GetByIdForRestaurantAsync(request.Id, restaurantId, ct)
            ?? throw new NotFoundException("Time slot not found.");

        var start = TimeOnly.Parse(request.StartTime);
        var end = TimeOnly.Parse(request.EndTime);

        var siblings = await timeSlots.GetByOfferingAsync(slot.OfferingId, ct);
        if (siblings.Any(s => s.Id != slot.Id && start < s.EndTime && s.StartTime < end))
        {
            throw new ConflictException("This slot overlaps with an existing one.");
        }

        slot.StartTime = start;
        slot.EndTime = end;
        slot.Capacity = request.Capacity;
        slot.BufferMinutes = request.BufferMinutes;

        await unitOfWork.SaveChangesAsync(ct);
    }
}
