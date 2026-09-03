using BuffetDiscovery.Application.Common;
using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Entities;
using FluentValidation;
using MediatR;

namespace BuffetDiscovery.Application.Features.Booking.Customer;

/// Converts an offered waitlist entry into a real booking once the customer confirms within
/// the offer window. Waitlist entries carry no confirmation code of their own, so the phone
/// number that made the entry doubles as the credential — the same account-free flow used
/// everywhere else on the customer side.
public record ConfirmWaitlistOfferCommand(int WaitlistId, string CustomerPhone) : IRequest<BookingDetailDto>;

public class ConfirmWaitlistOfferCommandValidator : AbstractValidator<ConfirmWaitlistOfferCommand>
{
    public ConfirmWaitlistOfferCommandValidator()
    {
        RuleFor(x => x.CustomerPhone).NotEmpty();
    }
}

public class ConfirmWaitlistOfferCommandHandler(
    IWaitlistRepository waitlistRepo,
    IBookingRepository bookingRepo,
    IRestaurantSettingsRepository settingsRepo,
    IUnitOfWork unitOfWork) : IRequestHandler<ConfirmWaitlistOfferCommand, BookingDetailDto>
{
    public async Task<BookingDetailDto> Handle(ConfirmWaitlistOfferCommand request, CancellationToken ct)
    {
        var entry = await waitlistRepo.GetByIdForCustomerAsync(request.WaitlistId, request.CustomerPhone.Trim(), ct)
            ?? throw new NotFoundException("Waitlist entry not found.");

        if (entry.Status != WaitlistStatus.Offered)
        {
            throw new ConflictException("This waitlist offer is no longer active.");
        }

        var service = entry.Service!;
        var settings = await settingsRepo.GetOrCreateAsync(service.RestaurantId, ct);
        var expiresAt = entry.NotifiedAt!.Value.AddMinutes(settings.WaitlistOfferWindowMinutes);
        if (DateTime.UtcNow > expiresAt)
        {
            entry.Status = WaitlistStatus.Expired;
            await unitOfWork.SaveChangesAsync(ct);
            throw new ConflictException("This waitlist offer has expired.");
        }

        string code;
        do
        {
            code = ConfirmationCodeGenerator.Generate(service.ServiceType);
        } while (await bookingRepo.ConfirmationCodeExistsAsync(code, ct));

        // A waitlist entry only records a head count, so everyone on it is priced as an
        // adult — the customer can't have split it into adults and children when joining.
        var booking = new Domain.Entities.Booking
        {
            ServiceId = entry.ServiceId,
            TimeSlotId = entry.TimeSlotId,
            Date = entry.Date,
            CustomerName = entry.CustomerName,
            CustomerPhone = entry.CustomerPhone,
            Adults = entry.PartySize,
            Children = 0,
            PartySize = entry.PartySize,
            TotalPrice = PriceCalculator.Total(service, entry.PartySize, 0),
            Status = BookingStatus.Confirmed,
            ConfirmationCode = code
        };
        bookingRepo.Add(booking);

        entry.Status = WaitlistStatus.Converted;

        await unitOfWork.SaveChangesAsync(ct);

        // Re-read so the response carries the full restaurant/location chain the booking
        // page shows, rather than whatever the waitlist entry happened to have loaded.
        var saved = await bookingRepo.GetByConfirmationCodeAsync(code, ct)!;
        return BookingMapper.ToDetail(saved!, settings.CancellationCutoffMinutes);
    }
}
