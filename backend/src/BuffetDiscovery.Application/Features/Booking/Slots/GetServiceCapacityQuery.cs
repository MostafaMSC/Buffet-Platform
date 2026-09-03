using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using MediatR;

namespace BuffetDiscovery.Application.Features.Booking.Slots;

public record GetServiceCapacityQuery(int ServiceId) : IRequest<ServiceCapacityDto>;

public class GetServiceCapacityQueryHandler(
    IServiceRepository services,
    ITimeSlotRepository timeSlots,
    ICurrentUserService currentUser) : IRequestHandler<GetServiceCapacityQuery, ServiceCapacityDto>
{
    public async Task<ServiceCapacityDto> Handle(GetServiceCapacityQuery request, CancellationToken ct)
    {
        var restaurantId = currentUser.RestaurantId ?? throw new UnauthorizedException("No restaurant associated with this account.");
        var service = await services.GetByIdForRestaurantAsync(request.ServiceId, restaurantId, ct)
            ?? throw new NotFoundException("Service not found.");

        var slots = await timeSlots.GetByServiceAsync(service.Id, ct);

        return new ServiceCapacityDto(
            service.Id,
            service.Capacity,
            slots.Select(s => new TimeSlotDto(s.Id, s.StartTime.ToString("HH:mm"), s.EndTime.ToString("HH:mm"), s.Capacity, s.BufferMinutes)).ToList()
        );
    }
}
