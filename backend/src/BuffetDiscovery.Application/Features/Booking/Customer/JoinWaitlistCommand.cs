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
    int OfferingId,
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
    IOfferingRepository offerings,
    ITimeSlotRepository timeSlots,
    IBookingRepository bookingRepo,
    IWaitlistRepository waitlistRepo,
    IAvailabilityRepository availability,
    IRestaurantSettingsRepository settingsRepo,
    WaitlistPromoter waitlistPromoter,
    IUnitOfWork unitOfWork) : IRequestHandler<JoinWaitlistCommand, WaitlistDetailDto>
{
    public async Task<WaitlistDetailDto> Handle(JoinWaitlistCommand request, CancellationToken ct)
    {
        var offering = await offerings.GetApprovedByIdAsync(request.OfferingId, ct)
            ?? throw new NotFoundException("Offering not found.");

        if (!RecurrenceEvaluator.MatchesRecurrence(offering, request.Date))
        {
            throw new ConflictException("This offering is not served on the selected date.");
        }

        var dayStatus = await availability.GetAsync(offering.Id, request.Date, ct);
        if (dayStatus is not null && !dayStatus.IsActive)
        {
            throw new ConflictException("This offering is not available on the selected date.");
        }

        Domain.Entities.TimeSlot? slot = null;
        int capacity;
        if (request.TimeSlotId.HasValue)
        {
            slot = await timeSlots.GetByIdAsync(request.TimeSlotId.Value, ct);
            if (slot is null || slot.IsDeleted || slot.OfferingId != offering.Id)
            {
                throw new NotFoundException("Time slot not found.");
            }
            capacity = slot.Capacity;
        }
        else
        {
            capacity = offering.Capacity ?? throw new ConflictException("This offering does not accept bookings.");
        }

        var settings = await settingsRepo.GetOrCreateAsync(offering.RestaurantId, ct);
        var effectiveCapacity = CapacityCalculator.EffectiveCapacity(capacity, settings.OverbookingTolerancePercent);

        await waitlistPromoter.ExpireAndPromoteAsync(request.TimeSlotId, offering.Id, offering.RestaurantId, request.Date, effectiveCapacity, ct);

        var booked = await bookingRepo.GetBookedPartySizeAsync(request.TimeSlotId, offering.Id, request.Date, ct);
        if (booked + request.PartySize <= effectiveCapacity)
        {
            throw new ConflictException("This slot still has room — please book directly instead of joining the waitlist.");
        }

        var position = await waitlistRepo.GetNextPositionAsync(request.TimeSlotId, offering.Id, request.Date, ct);

        var entry = new Domain.Entities.Waitlist
        {
            OfferingId = offering.Id,
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
            entry.Id, offering.RestaurantId, offering.Restaurant!.Name, offering.Restaurant!.NameAr,
            offering.Id, offering.MealType, entry.Date,
            slot?.StartTime.ToString("HH:mm"), slot?.EndTime.ToString("HH:mm"),
            entry.CustomerName, entry.CustomerPhone, entry.PartySize, entry.Position, entry.Status, entry.NotifiedAt,
            settings.WaitlistOfferWindowMinutes
        );
    }
}
