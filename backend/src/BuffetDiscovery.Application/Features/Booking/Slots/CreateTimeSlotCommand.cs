using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Entities;
using FluentValidation;
using MediatR;

namespace BuffetDiscovery.Application.Features.Booking.Slots;

public record CreateTimeSlotCommand(int ServiceId, string StartTime, string EndTime, int Capacity, int BufferMinutes) : IRequest<int>;

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
    IServiceRepository services,
    ITimeSlotRepository timeSlots,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateTimeSlotCommand, int>
{
    public async Task<int> Handle(CreateTimeSlotCommand request, CancellationToken ct)
    {
        var restaurantId = currentUser.RestaurantId ?? throw new UnauthorizedException("No restaurant associated with this account.");
        var service = await services.GetByIdForRestaurantAsync(request.ServiceId, restaurantId, ct)
            ?? throw new NotFoundException("Service not found.");

        var start = TimeOnly.Parse(request.StartTime);
        var end = TimeOnly.Parse(request.EndTime);

        var existing = await timeSlots.GetByServiceAsync(service.Id, ct);
        if (existing.Any(s => start < s.EndTime && s.StartTime < end))
        {
            throw new ConflictException("This slot overlaps with an existing one.");
        }

        var slot = new TimeSlot
        {
            ServiceId = service.Id,
            StartTime = start,
            EndTime = end,
            Capacity = request.Capacity,
            BufferMinutes = request.BufferMinutes
        };
        timeSlots.Add(slot);

        // An service is in exactly one mode: whole-window OR slots, never both.
        service.Capacity = null;

        await unitOfWork.SaveChangesAsync(ct);
        return slot.Id;
    }
}
