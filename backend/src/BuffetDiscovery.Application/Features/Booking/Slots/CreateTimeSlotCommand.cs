using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Entities;
using FluentValidation;
using MediatR;

namespace BuffetDiscovery.Application.Features.Booking.Slots;

public record CreateTimeSlotCommand(int OfferingId, string StartTime, string EndTime, int Capacity, int BufferMinutes) : IRequest<int>;

public class CreateTimeSlotCommandValidator : AbstractValidator<CreateTimeSlotCommand>
{
    public CreateTimeSlotCommandValidator()
    {
        RuleFor(x => x.StartTime).Must(t => TimeOnly.TryParse(t, out _)).WithMessage("Invalid time format, expected HH:mm.");
        RuleFor(x => x.EndTime).Must(t => TimeOnly.TryParse(t, out _)).WithMessage("Invalid time format, expected HH:mm.");
        RuleFor(x => x.Capacity).GreaterThan(0);
        RuleFor(x => x.BufferMinutes).GreaterThanOrEqualTo(0);
        RuleFor(x => x).Must(x => !TimeOnly.TryParse(x.StartTime, out var s) || !TimeOnly.TryParse(x.EndTime, out var e) || s < e)
            .WithMessage("Start time must be before end time.");
    }
}

public class CreateTimeSlotCommandHandler(
    IOfferingRepository offerings,
    ITimeSlotRepository timeSlots,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateTimeSlotCommand, int>
{
    public async Task<int> Handle(CreateTimeSlotCommand request, CancellationToken ct)
    {
        var restaurantId = currentUser.RestaurantId ?? throw new UnauthorizedException("No restaurant associated with this account.");
        var offering = await offerings.GetByIdForRestaurantAsync(request.OfferingId, restaurantId, ct)
            ?? throw new NotFoundException("Offering not found.");

        var start = TimeOnly.Parse(request.StartTime);
        var end = TimeOnly.Parse(request.EndTime);

        var existing = await timeSlots.GetByOfferingAsync(offering.Id, ct);
        if (existing.Any(s => start < s.EndTime && s.StartTime < end))
        {
            throw new ConflictException("This slot overlaps with an existing one.");
        }

        var slot = new TimeSlot
        {
            OfferingId = offering.Id,
            StartTime = start,
            EndTime = end,
            Capacity = request.Capacity,
            BufferMinutes = request.BufferMinutes
        };
        timeSlots.Add(slot);

        // An offering is in exactly one mode: whole-window OR slots, never both.
        offering.Capacity = null;

        await unitOfWork.SaveChangesAsync(ct);
        return slot.Id;
    }
}
