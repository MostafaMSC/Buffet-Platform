using BuffetDiscovery.Application.Common;
using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Entities;
using BuffetDiscovery.Domain.Services;
using FluentValidation;
using MediatR;

namespace BuffetDiscovery.Application.Features.Booking.Customer;

/// Party size is expressed as adults + children because that's what the price depends on.
/// Children young enough to eat free aren't counted here at all — they don't occupy a
/// paid seat and the booking form says so.
public record CreateBookingCommand(
    int ServiceId,
    int? TimeSlotId,
    DateOnly Date,
    string CustomerName,
    string CustomerPhone,
    int Adults,
    int Children = 0,
    string? CustomerEmail = null,
    string? SpecialRequests = null
) : IRequest<BookingDetailDto>;

public class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingCommandValidator()
    {
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CustomerPhone).NotEmpty().MaximumLength(30);
        RuleFor(x => x.CustomerEmail).MaximumLength(200).EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.CustomerEmail));
        RuleFor(x => x.SpecialRequests).MaximumLength(500);
        RuleFor(x => x.Adults).GreaterThan(0).WithMessage("A booking needs at least one adult.");
        RuleFor(x => x.Children).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Date).Must(d => d >= DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3)))
            .WithMessage("Cannot book a date in the past.");
    }
}

public class CreateBookingCommandHandler(
    IServiceRepository services,
    ITimeSlotRepository timeSlots,
    IBookingRepository bookingRepo,
    IAvailabilityRepository availability,
    IRestaurantSettingsRepository settingsRepo,
    ISearchRepository search,
    WaitlistPromoter waitlistPromoter,
    INotificationService notifications,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateBookingCommand, BookingDetailDto>
{
    public async Task<BookingDetailDto> Handle(CreateBookingCommand request, CancellationToken ct)
    {
        var service = await services.GetPublicByIdAsync(request.ServiceId, ct)
            ?? throw new NotFoundException("Service not found.", "service_not_found");

        var partySize = request.Adults + request.Children;
        var nowLocal = DateTime.UtcNow.AddHours(3); // Baghdad is UTC+3

        if (partySize < service.MinGuests)
        {
            throw new ConflictException($"This service takes bookings for {service.MinGuests} guests or more.", "booking_min_guests", new { min = service.MinGuests });
        }

        if (service.MaxGuests.HasValue && partySize > service.MaxGuests.Value)
        {
            throw new ConflictException($"This service takes bookings for up to {service.MaxGuests.Value} guests.", "booking_max_guests", new { max = service.MaxGuests.Value });
        }

        if (!RecurrenceEvaluator.MatchesRecurrence(service, request.Date))
        {
            throw new ConflictException("This service is not served on the selected date.", "not_served_on_date");
        }

        var dayStatus = await availability.GetAsync(service.Id, request.Date, ct);
        if (dayStatus is not null && !dayStatus.IsActive)
        {
            throw new ConflictException("This service is not available on the selected date.", "not_available_on_date");
        }

        var settings = await settingsRepo.GetOrCreateAsync(service.RestaurantId, ct);

        // Free the seats of any waitlist offer that has since expired before deciding
        // whether this party fits.
        var slotEntity = request.TimeSlotId.HasValue
            ? await timeSlots.GetByIdAsync(request.TimeSlotId.Value, ct)
            : null;

        if (request.TimeSlotId.HasValue && (slotEntity is null || slotEntity.IsDeleted || slotEntity.ServiceId != service.Id))
        {
            throw new NotFoundException("Time slot not found.", "slot_not_found");
        }

        // A slotted service always needs a slot to book against; falling through to
        // service.Capacity here would silently book the wrong thing (or, for a slotted
        // service with no whole-window capacity, throw a "doesn't accept bookings" error
        // that's wrong — the service does take bookings, the request just didn't say which
        // sitting).
        if (slotEntity is null && !service.Capacity.HasValue && service.TimeSlots.Any(s => !s.IsDeleted))
        {
            throw new ConflictException("Please choose a sitting time.", "choose_sitting");
        }

        var nominalCapacity = slotEntity?.Capacity ?? service.Capacity
            ?? throw new ConflictException("This service isn't set up to take bookings yet.", "not_bookable");

        await waitlistPromoter.ExpireAndPromoteAsync(
            request.TimeSlotId, service.Id, service.RestaurantId, request.Date,
            CapacityCalculator.EffectiveCapacity(nominalCapacity, settings.OverbookingTolerancePercent), ct);

        var booked = await search.GetBookedGuestsAsync([service.Id], request.Date, ct);
        var overrides = await search.GetSlotOverridesAsync([service.Id], request.Date, request.Date, ct);
        var slots = AvailabilityCalculator.Build(service, request.Date, booked, overrides, settings.OverbookingTolerancePercent);

        var slot = slots.FirstOrDefault(s => s.TimeSlotId == request.TimeSlotId)
            ?? throw new ConflictException("This sitting is not available.", "sitting_unavailable");

        if (slot.IsBlocked)
        {
            throw new ConflictException("This sitting has been closed by the restaurant.", "sitting_closed");
        }

        if (!slot.Fits(partySize))
        {
            throw slot.Remaining > 0
                ? new ConflictException($"Only {slot.Remaining} seats are left for this sitting.", "seats_left", new { count = slot.Remaining })
                : new ConflictException("This sitting is full. Join the waitlist instead.", "sitting_full");
        }

        if (AvailabilityCalculator.IsPast(slot, request.Date, nowLocal, service.MinAdvanceMinutes))
        {
            throw service.MinAdvanceMinutes > 0
                ? new ConflictException($"Bookings close {service.MinAdvanceMinutes} minutes before the sitting starts.", "advance_notice_required", new { minutes = service.MinAdvanceMinutes })
                : new ConflictException("This sitting has already started.", "sitting_started");
        }

        string code;
        do
        {
            code = ConfirmationCodeGenerator.Generate(service.ServiceType);
        } while (await bookingRepo.ConfirmationCodeExistsAsync(code, ct));

        // Request-mode services hold the booking until the restaurant accepts it. The seats
        // are still counted against capacity meanwhile, so a pending request can't be
        // double-sold while the restaurant decides.
        var status = service.BookingMode == BookingMode.Request
            ? BookingStatus.Pending
            : BookingStatus.Confirmed;

        var booking = new Domain.Entities.Booking
        {
            ServiceId = service.Id,
            TimeSlotId = request.TimeSlotId,
            Date = request.Date,
            CustomerName = request.CustomerName.Trim(),
            CustomerPhone = request.CustomerPhone.Trim(),
            CustomerEmail = request.CustomerEmail?.Trim(),
            SpecialRequests = request.SpecialRequests?.Trim(),
            Adults = request.Adults,
            Children = request.Children,
            PartySize = partySize,
            TotalPrice = PriceCalculator.Total(service, request.Adults, request.Children),
            Status = status,
            ConfirmationCode = code
        };
        bookingRepo.Add(booking);

        var whenText = slot.TimeSlotId is null ? "" : $" at {slot.StartTime:HH\\:mm}";
        await notifications.NotifyRestaurantAsync(
            service.RestaurantId,
            status == BookingStatus.Pending
                ? $"Booking request: {booking.CustomerName} ({partySize} guests) for {service.Name} on {request.Date:yyyy-MM-dd}{whenText}."
                : $"New booking: {booking.CustomerName} ({partySize} guests) for {service.Name} on {request.Date:yyyy-MM-dd}{whenText}.",
            status == BookingStatus.Pending
                ? $"طلب حجز: {booking.CustomerName} ({partySize} ضيوف) لـ {service.NameAr} بتاريخ {request.Date:yyyy-MM-dd}."
                : $"حجز جديد: {booking.CustomerName} ({partySize} ضيوف) لـ {service.NameAr} بتاريخ {request.Date:yyyy-MM-dd}.",
            ct);

        await unitOfWork.SaveChangesAsync(ct);

        booking.Service = service;
        booking.TimeSlot = slotEntity;
        return BookingMapper.ToDetail(booking, settings.CancellationCutoffMinutes);
    }
}
