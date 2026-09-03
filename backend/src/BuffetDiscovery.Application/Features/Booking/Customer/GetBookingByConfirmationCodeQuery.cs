using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using MediatR;

namespace BuffetDiscovery.Application.Features.Booking.Customer;

/// Backs the public "badge" page — the confirmation code is the only credential needed,
/// standing in for a customer account (see Phase 2 clarifying answer: no accounts, a
/// shareable code the customer can show restaurant staff).
public record GetBookingByConfirmationCodeQuery(string ConfirmationCode) : IRequest<BookingDetailDto>;

public class GetBookingByConfirmationCodeQueryHandler(IBookingRepository bookingRepo)
    : IRequestHandler<GetBookingByConfirmationCodeQuery, BookingDetailDto>
{
    public async Task<BookingDetailDto> Handle(GetBookingByConfirmationCodeQuery request, CancellationToken ct)
    {
        var booking = await bookingRepo.GetByConfirmationCodeAsync(request.ConfirmationCode, ct)
            ?? throw new NotFoundException("Booking not found.");

        var offering = booking.Offering!;
        var restaurant = offering.Restaurant!;

        return new BookingDetailDto(
            booking.Id, booking.ConfirmationCode, restaurant.Id, restaurant.Name, restaurant.NameAr,
            offering.Id, offering.MealType, booking.Date,
            booking.TimeSlot?.StartTime.ToString("HH:mm"), booking.TimeSlot?.EndTime.ToString("HH:mm"),
            booking.CustomerName, booking.CustomerPhone, booking.PartySize, booking.Status, booking.CreatedAt
        );
    }
}
