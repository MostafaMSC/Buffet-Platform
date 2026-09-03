using BuffetDiscovery.Application.Common;
using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Services;
using FluentValidation;
using MediatR;

namespace BuffetDiscovery.Application.Features.Booking.Customer;

public record CreateBookingCommand(
    int OfferingId,
    int? TimeSlotId,
    DateOnly Date,
    string CustomerName,
    string CustomerPhone,
    int PartySize
) : IRequest<BookingDetailDto>;

public class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingCommandValidator()
    {
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CustomerPhone).NotEmpty().MaximumLength(30);
        RuleFor(x => x.PartySize).GreaterThan(0);
        RuleFor(x => x.Date).Must(d => d >= DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3)))
            .WithMessage("Cannot book a date in the past.");
    }
}

public class CreateBookingCommandHandler(
    IOfferingRepository offerings,
    ITimeSlotRepository timeSlots,
    IBookingRepository bookingRepo,
    IAvailabilityRepository availability,
    IRestaurantSettingsRepository settingsRepo,
    WaitlistPromoter waitlistPromoter,
    INotificationService notifications,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateBookingCommand, BookingDetailDto>
{
    public async Task<BookingDetailDto> Handle(CreateBookingCommand request, CancellationToken ct)
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
        if (booked + request.PartySize > effectiveCapacity)
        {
            throw new ConflictException("This slot is full. Join the waitlist instead.");
        }

        string code;
        do
        {
            code = ConfirmationCodeGenerator.Generate();
        } while (await bookingRepo.ConfirmationCodeExistsAsync(code, ct));

        var booking = new Domain.Entities.Booking
        {
            OfferingId = offering.Id,
            TimeSlotId = request.TimeSlotId,
            Date = request.Date,
            CustomerName = request.CustomerName,
            CustomerPhone = request.CustomerPhone,
            PartySize = request.PartySize,
            Status = Domain.Entities.BookingStatus.Confirmed,
            ConfirmationCode = code
        };
        bookingRepo.Add(booking);

        await notifications.NotifyRestaurantAsync(
            offering.RestaurantId,
            $"New booking: {request.CustomerName} ({request.PartySize} people) on {request.Date:yyyy-MM-dd}.",
            $"حجز جديد: {request.CustomerName} ({request.PartySize} أشخاص) بتاريخ {request.Date:yyyy-MM-dd}.",
            ct);

        await unitOfWork.SaveChangesAsync(ct);

        return new BookingDetailDto(
            booking.Id, booking.ConfirmationCode, offering.RestaurantId, offering.Restaurant!.Name, offering.Restaurant!.NameAr,
            offering.Id, offering.MealType, booking.Date,
            slot?.StartTime.ToString("HH:mm"), slot?.EndTime.ToString("HH:mm"),
            booking.CustomerName, booking.CustomerPhone, booking.PartySize, booking.Status, booking.CreatedAt
        );
    }
}
