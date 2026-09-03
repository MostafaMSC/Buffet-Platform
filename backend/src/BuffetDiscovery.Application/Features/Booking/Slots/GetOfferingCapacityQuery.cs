using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using MediatR;

namespace BuffetDiscovery.Application.Features.Booking.Slots;

public record GetOfferingCapacityQuery(int OfferingId) : IRequest<OfferingCapacityDto>;

public class GetOfferingCapacityQueryHandler(
    IOfferingRepository offerings,
    ITimeSlotRepository timeSlots,
    ICurrentUserService currentUser) : IRequestHandler<GetOfferingCapacityQuery, OfferingCapacityDto>
{
    public async Task<OfferingCapacityDto> Handle(GetOfferingCapacityQuery request, CancellationToken ct)
    {
        var restaurantId = currentUser.RestaurantId ?? throw new UnauthorizedException("No restaurant associated with this account.");
        var offering = await offerings.GetByIdForRestaurantAsync(request.OfferingId, restaurantId, ct)
            ?? throw new NotFoundException("Offering not found.");

        var slots = await timeSlots.GetByOfferingAsync(offering.Id, ct);

        return new OfferingCapacityDto(
            offering.Id,
            offering.Capacity,
            slots.Select(s => new TimeSlotDto(s.Id, s.StartTime.ToString("HH:mm"), s.EndTime.ToString("HH:mm"), s.Capacity, s.BufferMinutes)).ToList()
        );
    }
}
