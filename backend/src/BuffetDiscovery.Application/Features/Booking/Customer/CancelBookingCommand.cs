using BuffetDiscovery.Application.Common;
using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Entities;
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
    INotificationService notifications,
    WaitlistPromoter waitlistPromoter,
    IUnitOfWork unitOfWork) : IRequestHandler<CancelBookingCommand>
{
    public async Task Handle(CancelBookingCommand request, CancellationToken ct)
    {
        var booking = await bookingRepo.GetByConfirmationCodeAsync(request.ConfirmationCode.Trim(), ct)
            ?? throw new NotFoundException("Booking not found.");

        // A request still awaiting the restaurant's answer can be withdrawn as freely as a
        // confirmed one; anything already cancelled, seated or closed out cannot.
        if (booking.Status is not (BookingStatus.Confirmed or BookingStatus.Pending))
        {
            throw new ConflictException("This booking can no longer be cancelled.");
        }

        var service = booking.Service!;
        var settings = await settingsRepo.GetOrCreateAsync(service.RestaurantId, ct);
        var cutoff = BookingMapper.CancellationCutoff(service, settings.CancellationCutoffMinutes);

        var bookingTime = booking.TimeSlot?.StartTime ?? service.OpensAt;
        var bookingDateTime = booking.Date.ToDateTime(bookingTime);
        var now = DateTime.UtcNow.AddHours(3); // Baghdad is UTC+3

        if (now > bookingDateTime.AddMinutes(-cutoff))
        {
            throw new ConflictException(
                $"Cancellations must be made at least {cutoff} minutes before the booking time.");
        }

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAt = DateTime.UtcNow;

        var capacity = booking.TimeSlot?.Capacity ?? service.Capacity ?? 0;
        var effectiveCapacity = CapacityCalculator.EffectiveCapacity(capacity, settings.OverbookingTolerancePercent);
        await waitlistPromoter.ExpireAndPromoteAsync(
            booking.TimeSlotId, booking.ServiceId, service.RestaurantId, booking.Date, effectiveCapacity, ct);

        await notifications.NotifyRestaurantAsync(
            service.RestaurantId,
            $"Booking cancelled: {booking.CustomerName} ({booking.PartySize} guests) for {service.Name} on {booking.Date:yyyy-MM-dd}.",
            $"إلغاء حجز: {booking.CustomerName} ({booking.PartySize} ضيوف) لـ {service.NameAr} بتاريخ {booking.Date:yyyy-MM-dd}.",
            ct);

        await unitOfWork.SaveChangesAsync(ct);
    }
}
