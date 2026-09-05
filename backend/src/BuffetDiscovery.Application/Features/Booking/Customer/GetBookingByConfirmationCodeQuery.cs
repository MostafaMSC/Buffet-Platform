using BuffetDiscovery.Application.Common;
using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using MediatR;

namespace BuffetDiscovery.Application.Features.Booking.Customer;

/// Backs the public booking page — the confirmation code is the only credential needed,
/// standing in for a customer account (no accounts: a shareable reference the customer can
/// show restaurant staff).
public record GetBookingByConfirmationCodeQuery(string ConfirmationCode) : IRequest<BookingDetailDto>;

public class GetBookingByConfirmationCodeQueryHandler(
    IBookingRepository bookingRepo,
    IServiceRepository serviceRepo,
    IRestaurantSettingsRepository settingsRepo)
    : IRequestHandler<GetBookingByConfirmationCodeQuery, BookingDetailDto>
{
    public async Task<BookingDetailDto> Handle(GetBookingByConfirmationCodeQuery request, CancellationToken ct)
    {
        var booking = await bookingRepo.GetByConfirmationCodeAsync(request.ConfirmationCode.Trim(), ct)
            ?? throw new NotFoundException("Booking not found.", "booking_not_found");

        var settings = await settingsRepo.GetOrCreateAsync(booking.Service!.RestaurantId, ct);
        var reviewed = await serviceRepo.GetReviewedBookingIdsAsync([booking.Id], ct);
        return BookingMapper.ToDetail(booking, settings.CancellationCutoffMinutes, reviewed.Contains(booking.Id));
    }
}
