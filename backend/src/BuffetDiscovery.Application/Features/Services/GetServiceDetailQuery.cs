using BuffetDiscovery.Application.Common;
using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Entities;
using BuffetDiscovery.Domain.Services;
using MediatR;

namespace BuffetDiscovery.Application.Features.Services;

/// Everything the detail page renders, in one request: the service, its menu, the
/// restaurant, live availability for the chosen date, a price quote for the chosen party,
/// reviews, and similar services to fall back to.
public record GetServiceDetailQuery(int ServiceId, DateOnly? Date = null, int Adults = 2, int Children = 0)
    : IRequest<ServiceDetailDto>;

public class GetServiceDetailQueryHandler(
    IServiceRepository services,
    ISearchRepository search,
    IRestaurantSettingsRepository settingsRepo,
    ISender mediator) : IRequestHandler<GetServiceDetailQuery, ServiceDetailDto>
{
    public async Task<ServiceDetailDto> Handle(GetServiceDetailQuery request, CancellationToken ct)
    {
        var service = await services.GetPublicByIdAsync(request.ServiceId, ct)
            ?? throw new NotFoundException("Service not found.");

        var restaurant = service.Restaurant!;
        var city = restaurant.Area!.City!;
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3));
        var date = request.Date ?? today;

        var settings = await settingsRepo.GetOrCreateAsync(service.RestaurantId, ct);
        var availability = await BuildAvailability(service, date, settings.OverbookingTolerancePercent,
            Math.Max(1, request.Adults + request.Children), ct);

        var ratings = await search.GetRatingsAsync([restaurant.Id], ct);
        var hasRating = ratings.TryGetValue(restaurant.Id, out var rating);

        var reviews = await services.GetReviewsAsync(restaurant.Id, service.Id, 12, ct);

        // Fall-back options if this one doesn't work out: same city, same kind of service.
        var similar = await mediator.Send(new Features.Search.SearchServicesQuery(
            Type: service.ServiceType,
            CitySlug: city.Slug,
            Availability: Features.Search.AvailabilityWindow.ThisWeek,
            Sort: Features.Search.SearchSort.Recommended,
            PageSize: 8), ct);

        return new ServiceDetailDto(
            service.Id,
            service.ServiceType,
            service.Name,
            service.NameAr,
            service.Description,
            service.DescriptionAr,
            service.MealType,
            FlagEnums.Cuisines(service.Cuisines),
            FlagEnums.Dietary(service.Dietary),

            service.PricingModel,
            service.PricePerAdult,
            service.PricePerChild,
            service.ChildAgeFrom,
            service.ChildAgeTo,
            service.FreeUnderAge,
            service.PackagePrice,
            service.PackageGuests,
            city.Country?.CurrencyCode ?? "IQD",

            service.MinGuests,
            service.MaxGuests,
            service.DurationMinutes,
            service.OpensAt.ToString("HH:mm"),
            service.ClosesAt.ToString("HH:mm"),
            service.Recurrence,
            WeekdayMapper.ToList(service.Weekdays).ToArray(),
            service.RamadanStartDate,
            service.RamadanEndDate,
            service.OneOffDate,

            service.BookingMode,
            service.MinAdvanceMinutes,
            BookingMapper.CancellationCutoff(service, settings.CancellationCutoffMinutes),

            service.Photos.OrderBy(p => p.SortOrder).Select(p => p.Url).ToList(),
            service.VideoUrl,
            service.MenuSections.OrderBy(m => m.SortOrder).Select(m => new MenuSectionDto(
                m.Id, m.Name, m.NameAr,
                m.Items.OrderBy(i => i.SortOrder).Select(i => new MenuItemDto(
                    i.Id, i.Name, i.NameAr, i.Description, i.DescriptionAr, FlagEnums.Dietary(i.Dietary))).ToList()
            )).ToList(),

            new RestaurantSummaryDto(
                restaurant.Id,
                restaurant.Name,
                restaurant.NameAr,
                restaurant.Description,
                restaurant.DescriptionAr,
                restaurant.PhoneNumber,
                restaurant.Address,
                restaurant.GoogleMapsUrl,
                restaurant.Latitude,
                restaurant.Longitude,
                restaurant.LogoUrl,
                restaurant.CoverPhotoUrl,
                restaurant.Area!.NameEn,
                restaurant.Area!.NameAr,
                city.NameEn,
                city.NameAr,
                city.Slug,
                FlagEnums.Features(restaurant.Features),
                hasRating ? Math.Round(rating.Average, 1) : null,
                hasRating ? rating.Count : 0),

            availability,
            Quote(service, request.Adults, request.Children, city.Country?.CurrencyCode ?? "IQD"),
            reviews.Select(r => new ReviewDto(r.Id, r.CustomerName, r.Rating, r.Comment, r.CreatedAt, r.BookingId.HasValue)).ToList(),
            similar.Items.Where(s => s.ServiceId != service.Id).Take(4).ToList());
    }

    private async Task<ServiceAvailabilityDto> BuildAvailability(
        Service service, DateOnly date, int tolerance, int guests, CancellationToken ct)
    {
        var isServed = RecurrenceEvaluator.MatchesRecurrence(service, date);
        var dayStatuses = await search.GetDayStatusesAsync([service.Id], date, date, ct);
        if (dayStatuses.TryGetValue((service.Id, date), out var isActive) && !isActive)
        {
            isServed = false;
        }

        if (!isServed)
        {
            return new ServiceAvailabilityDto(service.Id, date, false, service.TimeSlots.Any(s => !s.IsDeleted) || service.Capacity.HasValue, []);
        }

        var booked = await search.GetBookedGuestsAsync([service.Id], date, ct);
        var overrides = await search.GetSlotOverridesAsync([service.Id], date, date, ct);
        var slots = AvailabilityCalculator.Build(service, date, booked, overrides, tolerance);
        var nowLocal = DateTime.UtcNow.AddHours(3);

        return new ServiceAvailabilityDto(
            service.Id,
            date,
            true,
            slots.Count > 0,
            slots.Select(s => new ServiceSlotDto(
                s.TimeSlotId,
                s.StartTime.ToString("HH:mm"),
                s.EndTime.ToString("HH:mm"),
                s.Capacity,
                s.Booked,
                s.Remaining,
                s.IsFull,
                s.Fits(guests),
                AvailabilityCalculator.IsPast(s, date, nowLocal, service.MinAdvanceMinutes)
            )).ToList());
    }

    private static PriceQuoteDto Quote(Service service, int adults, int children, string currency)
    {
        var childUnit = service.PricePerChild ?? service.PricePerAdult;

        if (service.PricingModel == PricingModel.PerPackage)
        {
            var packageGuests = service.PackageGuests is > 0 ? service.PackageGuests.Value : 1;
            var packages = (int)Math.Ceiling(Math.Max(adults + children, 1) / (double)packageGuests);
            return new PriceQuoteDto(
                service.PricingModel, adults, children, 0, 0, 0, 0,
                packages, service.PackagePrice,
                PriceCalculator.Total(service, adults, children), currency);
        }

        return new PriceQuoteDto(
            service.PricingModel,
            adults,
            children,
            service.PricePerAdult,
            childUnit,
            service.PricePerAdult * adults,
            childUnit * children,
            null,
            null,
            PriceCalculator.Total(service, adults, children),
            currency);
    }
}
