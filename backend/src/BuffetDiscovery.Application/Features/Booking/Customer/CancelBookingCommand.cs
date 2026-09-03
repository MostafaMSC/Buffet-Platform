using BuffetDiscovery.Application.Common;
using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace BuffetDiscovery.Application.Features.Booking.Customer;

public record CancelBookingCommand(string ConfirmationCode) : IRequest;

public class CancelBookingCommandValidator : AbstractValidator<CancelBookingCommand>
{
    public CancelBookingCommandValidator()
    {
        RuleFor(x => x.ConfirmationCode).NotEmpty();
    }
}

public class CancelBookingCommandHandler(
    IBookingRepository bookingRepo,
    IRestaurantSettingsRepository settingsRepo,
    WaitlistPromoter waitlistPromoter,
    IUnitOfWork unitOfWork) : IRequestHandler<CancelBookingCommand>
{
    public async Task Handle(CancelBookingCommand request, CancellationToken ct)
    {
        var booking = await bookingRepo.GetByConfirmationCodeAsync(request.ConfirmationCode, ct)
            ?? throw new NotFoundException("Booking not found.");

        if (booking.Status != Domain.Entities.BookingStatus.Confirmed)
        {
            throw new ConflictException("This booking can no longer be cancelled.");
        }

        var settings = await settingsRepo.GetOrCreateAsync(booking.Offering!.RestaurantId, ct);
        var bookingTime = booking.TimeSlot?.StartTime ?? booking.Offering!.OpensAt;
        var bookingDateTime = booking.Date.ToDateTime(bookingTime);
        var now = DateTime.UtcNow.AddHours(3); // Baghdad is UTC+3

        if (now > bookingDateTime.AddMinutes(-settings.CancellationCutoffMinutes))
        {
            throw new ConflictException(
                $"Cancellations must be made at least {settings.CancellationCutoffMinutes} minutes before the booking time.");
        }

        booking.Status = Domain.Entities.BookingStatus.Cancelled;
        booking.CancelledAt = DateTime.UtcNow;

        var capacity = booking.TimeSlot?.Capacity ?? booking.Offering!.Capacity ?? 0;
        var effectiveCapacity = CapacityCalculator.EffectiveCapacity(capacity, settings.OverbookingTolerancePercent);
        await waitlistPromoter.ExpireAndPromoteAsync(
            booking.TimeSlotId, booking.OfferingId, booking.Offering!.RestaurantId, booking.Date, effectiveCapacity, ct);

        await unitOfWork.SaveChangesAsync(ct);
    }
}
