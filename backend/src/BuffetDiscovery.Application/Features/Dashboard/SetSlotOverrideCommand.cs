using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Entities;
using FluentValidation;
using MediatR;

namespace BuffetDiscovery.Application.Features.Dashboard;

/// Blocks a sitting or changes its capacity for one date — the calendar's edit action.
/// Passing neither a block nor a capacity clears the exception and returns the slot to its
/// standard capacity.
public record SetSlotOverrideCommand(
    int TimeSlotId,
    DateOnly Date,
    bool IsBlocked = false,
    int? Capacity = null,
    string? Note = null
) : IRequest;

public class SetSlotOverrideCommandValidator : AbstractValidator<SetSlotOverrideCommand>
{
    public SetSlotOverrideCommandValidator()
    {
        RuleFor(x => x.Capacity).GreaterThanOrEqualTo(0).When(x => x.Capacity.HasValue);
        RuleFor(x => x.Note).MaximumLength(200);
    }
}

public class SetSlotOverrideCommandHandler(
    ITimeSlotRepository timeSlots,
    ISlotOverrideRepository overrides,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<SetSlotOverrideCommand>
{
    public async Task Handle(SetSlotOverrideCommand request, CancellationToken ct)
    {
        var restaurantId = currentUser.RestaurantId ?? throw new UnauthorizedException("No restaurant associated with this account.");
        var slot = await timeSlots.GetByIdForRestaurantAsync(request.TimeSlotId, restaurantId, ct)
            ?? throw new NotFoundException("Time slot not found.");

        var existing = await overrides.GetAsync(slot.Id, request.Date, ct);
        var isDefault = !request.IsBlocked && request.Capacity is null && string.IsNullOrWhiteSpace(request.Note);

        if (isDefault)
        {
            if (existing is not null) overrides.Remove(existing);
            await unitOfWork.SaveChangesAsync(ct);
            return;
        }

        if (existing is null)
        {
            overrides.Add(new SlotOverride
            {
                TimeSlotId = slot.Id,
                Date = request.Date,
                IsBlocked = request.IsBlocked,
                Capacity = request.Capacity,
                Note = request.Note?.Trim()
            });
        }
        else
        {
            existing.IsBlocked = request.IsBlocked;
            existing.Capacity = request.Capacity;
            existing.Note = request.Note?.Trim();
        }

        await unitOfWork.SaveChangesAsync(ct);
    }
}
