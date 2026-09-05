using BuffetDiscovery.Application.Common;
using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using MediatR;

namespace BuffetDiscovery.Application.Features.Dashboard;

/// Loads one service in full so the restaurant's editor can round-trip every field it
/// shows, including the menu and time slots.
public record GetServiceEditorQuery(int Id) : IRequest<ServiceEditorDto>;

public class GetServiceEditorQueryHandler(
    IServiceRepository services,
    ICurrentUserService currentUser) : IRequestHandler<GetServiceEditorQuery, ServiceEditorDto>
{
    public async Task<ServiceEditorDto> Handle(GetServiceEditorQuery request, CancellationToken ct)
    {
        var restaurantId = currentUser.RestaurantId ?? throw new UnauthorizedException("No restaurant associated with this account.");
        var service = await services.GetByIdForRestaurantAsync(request.Id, restaurantId, ct)
            ?? throw new NotFoundException("Service not found.");

        return new ServiceEditorDto(
            service.Id,
            service.ServiceType,
            service.Name,
            service.NameAr,
            service.Description,
            service.DescriptionAr,
            service.MealType,
            FlagEnums.Cuisines(service.Cuisines),
            FlagEnums.Dietary(service.Dietary),
            service.Status,

            service.PricingModel,
            service.PricePerAdult,
            service.PricePerChild,
            service.ChildAgeFrom,
            service.ChildAgeTo,
            service.FreeUnderAge,
            service.PackagePrice,
            service.PackageGuests,

            service.MinGuests,
            service.MaxGuests,
            service.DurationMinutes,

            service.OpensAt.ToString("HH:mm"),
            service.ClosesAt.ToString("HH:mm"),
            service.Recurrence,
            WeekdayMapper.ToList(service.Weekdays),
            service.RamadanStartDate,
            service.RamadanEndDate,
            service.OneOffDate,

            service.BookingMode,
            service.MinAdvanceMinutes,
            service.CancellationCutoffMinutes,
            service.Capacity,

            service.Photos.OrderBy(p => p.SortOrder).Select(p => p.Url).ToList(),
            service.VideoUrl,
            service.TimeSlots.Where(s => !s.IsDeleted).OrderBy(s => s.StartTime)
                .Select(s => new TimeSlotDto(s.Id, s.StartTime.ToString("HH:mm"), s.EndTime.ToString("HH:mm"), s.Capacity, s.BufferMinutes))
                .ToList(),
            service.MenuSections.OrderBy(m => m.SortOrder).Select(m => new MenuSectionDto(
                m.Id, m.Name, m.NameAr,
                m.Items.OrderBy(i => i.SortOrder).Select(i => new MenuItemDto(
                    i.Id, i.Name, i.NameAr, i.Description, i.DescriptionAr, FlagEnums.Dietary(i.Dietary))).ToList()
            )).ToList());
    }
}
