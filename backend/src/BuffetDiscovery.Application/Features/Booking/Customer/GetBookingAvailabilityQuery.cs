using BuffetDiscovery.Application.Common;
using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Services;
using MediatR;

namespace BuffetDiscovery.Application.Features.Booking.Customer;

public record GetBookingAvailabilityQuery(int OfferingId, DateOnly Date) : IRequest<BookingAvailabilityDto>;

public class GetBookingAvailabilityQueryHandler(
    IOfferingRepository offerings,
    ITimeSlotRepository timeSlots,
    IBookingRepository bookingRepo,
    IWaitlistRepository waitlistRepo,
    IAvailabilityRepository availability,
    IRestaurantSettingsRepository settingsRepo,
    WaitlistPromoter waitlistPromoter,
    IUnitOfWork unitOfWork) : IRequestHandler<GetBookingAvailabilityQuery, BookingAvailabilityDto>
{
    public async Task<BookingAvailabilityDto> Handle(GetBookingAvailabilityQuery request, CancellationToken ct)
    {
        var offering = await offerings.GetApprovedByIdAsync(request.OfferingId, ct)
            ?? throw new NotFoundException("Offering not found.");

        // Same day-matching rule CreateBookingCommand/JoinWaitlistCommand enforce — a day
        // the offering doesn't run (or was toggled off) has nothing to show as bookable,
        // regardless of capacity, so the widget and the actual booking check agree.
        var matchesRecurrence = RecurrenceEvaluator.MatchesRecurrence(offering, request.Date);
        var dayStatus = await availability.GetAsync(offering.Id, request.Date, ct);
        var isActiveToday = dayStatus?.IsActive ?? matchesRecurrence;
        if (!matchesRecurrence || !isActiveToday)
        {
            return new BookingAvailabilityDto(offering.Id, request.Date, false, []);
        }

        var slots = await timeSlots.GetByOfferingAsync(offering.Id, ct);
        var settings = await settingsRepo.GetOrCreateAsync(offering.RestaurantId, ct);

        var result = new List<SlotAvailabilityDto>();

        if (slots.Count > 0)
        {
            foreach (var slot in slots)
            {
                var effectiveCapacity = CapacityCalculator.EffectiveCapacity(slot.Capacity, settings.OverbookingTolerancePercent);
                await waitlistPromoter.ExpireAndPromoteAsync(slot.Id, offering.Id, offering.RestaurantId, request.Date, effectiveCapacity, ct);

                var booked = await bookingRepo.GetBookedPartySizeAsync(slot.Id, offering.Id, request.Date, ct);
                var queue = await waitlistRepo.GetQueueAsync(slot.Id, offering.Id, request.Date, ct);

                result.Add(new SlotAvailabilityDto(
                    slot.Id, slot.StartTime.ToString("HH:mm"), slot.EndTime.ToString("HH:mm"),
                    slot.Capacity, booked, Math.Max(0, effectiveCapacity - booked), booked >= effectiveCapacity,
                    queue.Count(w => w.Status == Domain.Entities.WaitlistStatus.Waiting)
                ));
            }
        }
        else if (offering.Capacity.HasValue)
        {
            var effectiveCapacity = CapacityCalculator.EffectiveCapacity(offering.Capacity.Value, settings.OverbookingTolerancePercent);
            await waitlistPromoter.ExpireAndPromoteAsync(null, offering.Id, offering.RestaurantId, request.Date, effectiveCapacity, ct);

            var booked = await bookingRepo.GetBookedPartySizeAsync(null, offering.Id, request.Date, ct);
            var queue = await waitlistRepo.GetQueueAsync(null, offering.Id, request.Date, ct);

            result.Add(new SlotAvailabilityDto(
                null, offering.OpensAt.ToString("HH:mm"), offering.ClosesAt.ToString("HH:mm"),
                offering.Capacity.Value, booked, Math.Max(0, effectiveCapacity - booked), booked >= effectiveCapacity,
                queue.Count(w => w.Status == Domain.Entities.WaitlistStatus.Waiting)
            ));
        }

        await unitOfWork.SaveChangesAsync(ct);

        return new BookingAvailabilityDto(offering.Id, request.Date, result.Count > 0, result);
    }
}
