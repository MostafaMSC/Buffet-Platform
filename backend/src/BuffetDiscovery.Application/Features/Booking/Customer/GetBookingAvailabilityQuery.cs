using BuffetDiscovery.Application.Common;
using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Services;
using MediatR;

namespace BuffetDiscovery.Application.Features.Booking.Customer;

/// Live availability for one service on one date — what the booking widget re-reads every
/// time the customer changes the date or party size.
public record GetBookingAvailabilityQuery(int ServiceId, DateOnly Date, int Guests = 1)
    : IRequest<ServiceAvailabilityDto>;

public class GetBookingAvailabilityQueryHandler(
    IServiceRepository services,
    ISearchRepository search,
    IRestaurantSettingsRepository settingsRepo,
    WaitlistPromoter waitlistPromoter,
    IUnitOfWork unitOfWork) : IRequestHandler<GetBookingAvailabilityQuery, ServiceAvailabilityDto>
{
    public async Task<ServiceAvailabilityDto> Handle(GetBookingAvailabilityQuery request, CancellationToken ct)
    {
        var service = await services.GetPublicByIdAsync(request.ServiceId, ct)
            ?? throw new NotFoundException("Service not found.");

        var bookingEnabled = service.TimeSlots.Any(s => !s.IsDeleted) || service.Capacity.HasValue;

        // The same day-matching rule booking enforces — a day the service doesn't run has
        // nothing to show as bookable, so the widget and the booking call agree.
        var isServed = RecurrenceEvaluator.MatchesRecurrence(service, request.Date);
        var dayStatuses = await search.GetDayStatusesAsync([service.Id], request.Date, request.Date, ct);
        if (dayStatuses.TryGetValue((service.Id, request.Date), out var isActive) && !isActive)
        {
            isServed = false;
        }

        if (!isServed)
        {
            return new ServiceAvailabilityDto(service.Id, request.Date, false, bookingEnabled, []);
        }

        var settings = await settingsRepo.GetOrCreateAsync(service.RestaurantId, ct);
        var guests = Math.Max(1, request.Guests);
        var nowLocal = DateTime.UtcNow.AddHours(3);

        var booked = await search.GetBookedGuestsAsync([service.Id], request.Date, ct);
        var overrides = await search.GetSlotOverridesAsync([service.Id], request.Date, request.Date, ct);
        var slots = AvailabilityCalculator.Build(service, request.Date, booked, overrides, settings.OverbookingTolerancePercent);

        // Opportunistically retire expired waitlist offers so seats they were holding show
        // up as free here rather than waiting for a background job this codebase doesn't run.
        foreach (var slot in slots)
        {
            await waitlistPromoter.ExpireAndPromoteAsync(
                slot.TimeSlotId, service.Id, service.RestaurantId, request.Date, slot.EffectiveCapacity, ct);
        }
        await unitOfWork.SaveChangesAsync(ct);

        return new ServiceAvailabilityDto(
            service.Id,
            request.Date,
            true,
            bookingEnabled,
            slots.Select(s => new ServiceSlotDto(
                s.TimeSlotId,
                s.StartTime.ToString("HH:mm"),
                s.EndTime.ToString("HH:mm"),
                s.Capacity,
                s.Booked,
                s.Remaining,
                s.IsFull,
                s.Fits(guests),
                AvailabilityCalculator.IsPast(s, request.Date, nowLocal, service.MinAdvanceMinutes)
            )).ToList());
    }
}
