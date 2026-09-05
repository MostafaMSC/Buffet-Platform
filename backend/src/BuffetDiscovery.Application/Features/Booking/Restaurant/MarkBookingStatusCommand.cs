using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Entities;
using FluentValidation;
using MediatR;

namespace BuffetDiscovery.Application.Features.Booking.Restaurant;

/// The restaurant's actions on a booking through its life: accept or decline a request,
/// seat the guests on arrival, or close it out afterwards as completed or a no-show.
public record MarkBookingStatusCommand(int BookingId, BookingStatus Status) : IRequest;

public class MarkBookingStatusCommandValidator : AbstractValidator<MarkBookingStatusCommand>
{
    private static readonly BookingStatus[] Allowed =
    [
        BookingStatus.Confirmed, BookingStatus.Rejected, BookingStatus.CheckedIn,
        BookingStatus.Completed, BookingStatus.NoShow, BookingStatus.Cancelled
    ];

    public MarkBookingStatusCommandValidator()
    {
        RuleFor(x => x.Status).Must(s => Allowed.Contains(s))
            .WithMessage("That is not a status a restaurant can set.");
    }
}

public class MarkBookingStatusCommandHandler(
    IBookingRepository bookingRepo,
    INotificationService notifications,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<MarkBookingStatusCommand>
{
    /// Which statuses each current status can move to. Keeping this explicit stops a
    /// cancelled booking being quietly resurrected, or a no-show being re-seated.
    private static readonly Dictionary<BookingStatus, BookingStatus[]> AllowedTransitions = new()
    {
        [BookingStatus.Pending] = [BookingStatus.Confirmed, BookingStatus.Rejected, BookingStatus.Cancelled],
        [BookingStatus.Confirmed] = [BookingStatus.CheckedIn, BookingStatus.Completed, BookingStatus.NoShow, BookingStatus.Cancelled],
        [BookingStatus.CheckedIn] = [BookingStatus.Completed, BookingStatus.NoShow],
    };

    public async Task Handle(MarkBookingStatusCommand request, CancellationToken ct)
    {
        var restaurantId = currentUser.RestaurantId ?? throw new UnauthorizedException("No restaurant associated with this account.");
        var booking = await bookingRepo.GetByIdForRestaurantAsync(request.BookingId, restaurantId, ct)
            ?? throw new NotFoundException("Booking not found.");

        if (!AllowedTransitions.TryGetValue(booking.Status, out var allowed) || !allowed.Contains(request.Status))
        {
            throw new ConflictException($"A {booking.Status} booking cannot be marked {request.Status}.");
        }

        booking.Status = request.Status;

        if (request.Status == BookingStatus.CheckedIn)
        {
            booking.CheckedInAt = DateTime.UtcNow;
        }

        if (request.Status is BookingStatus.Cancelled or BookingStatus.Rejected)
        {
            booking.CancelledAt = DateTime.UtcNow;
        }

        // The customer has no live channel yet; this is the call site a WhatsApp/SMS
        // implementation slots into, and meanwhile they see the change on their booking page.
        if (request.Status is BookingStatus.Confirmed or BookingStatus.Rejected)
        {
            await notifications.NotifyCustomerAsync(
                booking.CustomerPhone,
                request.Status == BookingStatus.Confirmed
                    ? $"Your booking {booking.ConfirmationCode} has been confirmed."
                    : $"Your booking request {booking.ConfirmationCode} could not be accepted.",
                ct);
        }

        await unitOfWork.SaveChangesAsync(ct);
    }
}
