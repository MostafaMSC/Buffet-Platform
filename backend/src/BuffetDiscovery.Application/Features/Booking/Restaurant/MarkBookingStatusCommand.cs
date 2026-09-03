using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Entities;
using FluentValidation;
using MediatR;

namespace BuffetDiscovery.Application.Features.Booking.Restaurant;

/// Restaurant staff mark a booking No-show or Completed after the fact (used for the
/// no-show-rate analytics below). Any other status transition goes through the customer's
/// own cancel flow instead.
public record MarkBookingStatusCommand(int BookingId, BookingStatus Status) : IRequest;

public class MarkBookingStatusCommandValidator : AbstractValidator<MarkBookingStatusCommand>
{
    public MarkBookingStatusCommandValidator()
    {
        RuleFor(x => x.Status).Must(s => s is BookingStatus.NoShow or BookingStatus.Completed)
            .WithMessage("Status must be NoShow or Completed.");
    }
}

public class MarkBookingStatusCommandHandler(
    IBookingRepository bookingRepo,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<MarkBookingStatusCommand>
{
    public async Task Handle(MarkBookingStatusCommand request, CancellationToken ct)
    {
        var restaurantId = currentUser.RestaurantId ?? throw new UnauthorizedException("No restaurant associated with this account.");
        var booking = await bookingRepo.GetByIdForRestaurantAsync(request.BookingId, restaurantId, ct)
            ?? throw new NotFoundException("Booking not found.");

        if (booking.Status != BookingStatus.Confirmed)
        {
            throw new ConflictException("Only confirmed bookings can be marked no-show or completed.");
        }

        booking.Status = request.Status;
        await unitOfWork.SaveChangesAsync(ct);
    }
}
