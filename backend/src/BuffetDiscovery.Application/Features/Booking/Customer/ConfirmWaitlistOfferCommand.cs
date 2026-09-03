using BuffetDiscovery.Application.Common;
using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace BuffetDiscovery.Application.Features.Booking.Customer;

/// Converts an Offered waitlist entry into a real Booking once the customer confirms
/// within the offer window. Waitlist entries carry no confirmation code of their own
/// (only real Bookings do), so the phone number that made the entry doubles as the
/// credential here — same anonymous, account-free flow as everything else in Phase 2.
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
        var entry = await waitlistRepo.GetByIdForCustomerAsync(request.WaitlistId, request.CustomerPhone, ct)
            ?? throw new NotFoundException("Waitlist entry not found.");

        if (entry.Status != Domain.Entities.WaitlistStatus.Offered)
        {
            throw new ConflictException("This waitlist offer is no longer active.");
        }

        var offering = entry.Offering!;
        var settings = await settingsRepo.GetOrCreateAsync(offering.RestaurantId, ct);
        var expiresAt = entry.NotifiedAt!.Value.AddMinutes(settings.WaitlistOfferWindowMinutes);
        if (DateTime.UtcNow > expiresAt)
        {
            entry.Status = Domain.Entities.WaitlistStatus.Expired;
            await unitOfWork.SaveChangesAsync(ct);
            throw new ConflictException("This waitlist offer has expired.");
        }

        string code;
        do
        {
            code = ConfirmationCodeGenerator.Generate();
        } while (await bookingRepo.ConfirmationCodeExistsAsync(code, ct));

        var booking = new Domain.Entities.Booking
        {
            OfferingId = entry.OfferingId,
            TimeSlotId = entry.TimeSlotId,
            Date = entry.Date,
            CustomerName = entry.CustomerName,
            CustomerPhone = entry.CustomerPhone,
            PartySize = entry.PartySize,
            Status = Domain.Entities.BookingStatus.Confirmed,
            ConfirmationCode = code
        };
        bookingRepo.Add(booking);

        entry.Status = Domain.Entities.WaitlistStatus.Converted;

        await unitOfWork.SaveChangesAsync(ct);

        return new BookingDetailDto(
            booking.Id, booking.ConfirmationCode, offering.RestaurantId, offering.Restaurant!.Name, offering.Restaurant!.NameAr,
            offering.Id, offering.MealType, booking.Date,
            entry.TimeSlot?.StartTime.ToString("HH:mm"), entry.TimeSlot?.EndTime.ToString("HH:mm"),
            booking.CustomerName, booking.CustomerPhone, booking.PartySize, booking.Status, booking.CreatedAt
        );
    }
}
