using BuffetDiscovery.Application.Common;
using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Services;
using FluentValidation;
using MediatR;

namespace BuffetDiscovery.Application.Features.Booking.Customer;

/// An explicit customer action, distinct from CreateBookingCommand — a customer chooses to
/// join the waitlist only once they've seen the slot is already full via
/// GetBookingAvailabilityQuery. Rejecting the join when there's still room keeps the queue
/// meaningful (no one occupies a waitlist slot they could've just booked outright).
public record JoinWaitlistCommand(
    int ServiceId,
    int? TimeSlotId,
    DateOnly Date,
    string CustomerName,
    string CustomerPhone,
    int PartySize
) : IRequest<WaitlistDetailDto>;

public class JoinWaitlistCommandValidator : AbstractValidator<JoinWaitlistCommand>
{
    public JoinWaitlistCommandValidator()
    {
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CustomerPhone).NotEmpty().MaximumLength(30);
        RuleFor(x => x.PartySize).GreaterThan(0);
        RuleFor(x => x.Date).Must(d => d >= DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3)))
            .WithMessage("Cannot join the waitlist for a date in the past.");
    }
}

public class JoinWaitlistCommandHandler(
    IServiceRepository services,
    ITimeSlotRepository timeSlots,
    IWaitlistRepository waitlistRepo,
    IAvailabilityRepository availability,
    IRestaurantSettingsRepository settingsRepo,
    ISearchRepository search,
    WaitlistPromoter waitlistPromoter,
    IUnitOfWork unitOfWork) : IRequestHandler<JoinWaitlistCommand, WaitlistDetailDto>
{
    public async Task<WaitlistDetailDto> Handle(JoinWaitlistCommand request, CancellationToken ct)
    {
        var service = await services.GetPublicByIdAsync(request.ServiceId, ct)
            ?? throw new NotFoundException("Service not found.", "service_not_found");

        if (!RecurrenceEvaluator.MatchesRecurrence(service, request.Date))
        {
            throw new ConflictException("This service is not served on the selected date.", "not_served_on_date");
        }

        var dayStatus = await availability.GetAsync(service.Id, request.Date, ct);
        if (dayStatus is not null && !dayStatus.IsActive)
        {
            throw new ConflictException("This service is not available on the selected date.", "not_available_on_date");
        }

        Domain.Entities.TimeSlot? slot = null;
        if (request.TimeSlotId.HasValue)
        {
            slot = await timeSlots.GetByIdAsync(request.TimeSlotId.Value, ct);
            if (slot is null || slot.IsDeleted || slot.ServiceId != service.Id)
            {
                throw new NotFoundException("Time slot not found.", "slot_not_found");
            }
        }
        else if (service.TimeSlots.Any(s => !s.IsDeleted))
        {
            // A slotted service always needs to know which sitting to queue for — falling
            // through to service.Capacity here would throw "doesn't accept bookings" for a
            // service that plainly does, just wasn't told which slot.
            throw new ConflictException("Please choose a sitting time to join the waitlist for.", "choose_sitting_waitlist");
        }
        else if (!service.Capacity.HasValue)
        {
            throw new ConflictException("This service isn't set up to take bookings yet.", "not_bookable");
        }

        var settings = await settingsRepo.GetOrCreateAsync(service.RestaurantId, ct);

        // Read the same picture of the day AvailabilityCalculator gives every other
        // caller, so "is this slot actually full" agrees with what the guest just saw on
        // the detail page — including any capacity the restaurant overrode for this date.
        var booked = await search.GetBookedGuestsAsync([service.Id], request.Date, ct);
        var overrides = await search.GetSlotOverridesAsync([service.Id], request.Date, request.Date, ct);
        var slots = AvailabilityCalculator.Build(service, request.Date, booked, overrides, settings.OverbookingTolerancePercent);
        var slotAvailability = slots.FirstOrDefault(s => s.TimeSlotId == request.TimeSlotId)
            ?? throw new ConflictException("This sitting is not available.", "sitting_unavailable");

        await waitlistPromoter.ExpireAndPromoteAsync(request.TimeSlotId, service.Id, service.RestaurantId, request.Date, slotAvailability.EffectiveCapacity, ct);

        if (slotAvailability.Fits(request.PartySize))
        {
            throw new ConflictException("This slot still has room — please book directly instead of joining the waitlist.", "slot_has_room");
        }

        var position = await waitlistRepo.GetNextPositionAsync(request.TimeSlotId, service.Id, request.Date, ct);

        var entry = new Domain.Entities.Waitlist
        {
            ServiceId = service.Id,
            TimeSlotId = request.TimeSlotId,
            Date = request.Date,
            CustomerName = request.CustomerName,
            CustomerPhone = request.CustomerPhone,
            PartySize = request.PartySize,
            Position = position,
            Status = Domain.Entities.WaitlistStatus.Waiting
        };
        waitlistRepo.Add(entry);

        await unitOfWork.SaveChangesAsync(ct);

        return new WaitlistDetailDto(
            entry.Id, service.RestaurantId, service.Restaurant!.Name, service.Restaurant!.NameAr,
            service.Id, service.MealType, entry.Date,
            slot?.StartTime.ToString("HH:mm"), slot?.EndTime.ToString("HH:mm"),
            entry.CustomerName, entry.CustomerPhone, entry.PartySize, entry.Position, entry.Status, entry.NotifiedAt,
            settings.WaitlistOfferWindowMinutes
        );
    }
}
