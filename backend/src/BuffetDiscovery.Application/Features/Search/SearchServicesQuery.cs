using BuffetDiscovery.Application.Common;
using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Entities;
using BuffetDiscovery.Domain.Services;
using MediatR;

namespace BuffetDiscovery.Application.Features.Search;

public enum SearchSort
{
    Recommended,
    PriceLowToHigh,
    PriceHighToLow,
    Rating,
    Distance,
    Popular
}

public enum AvailabilityWindow
{
    SelectedDate,
    Today,
    ThisWeek,
    Any
}

public enum TimeOfDay
{
    Morning,
    Lunch,
    Afternoon,
    Dinner,
    LateNight
}

/// One request carries the whole search. Every filter the UI offers is answered here, so
/// changing a filter is a single round trip rather than a cascade of calls.
public record SearchServicesQuery(
    ServiceType? Type = null,
    string? CitySlug = null,
    int? AreaId = null,
    DateOnly? Date = null,
    TimeOnly? Time = null,
    TimeOfDay? TimeOfDay = null,
    int? Guests = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string[]? Cuisines = null,
    string[]? Dietary = null,
    string[]? Features = null,
    string[]? MealTypes = null,
    BookingMode? BookingMode = null,
    double? MinRating = null,
    AvailabilityWindow Availability = AvailabilityWindow.SelectedDate,
    SearchSort Sort = SearchSort.Recommended,
    double? Lat = null,
    double? Lng = null,
    double? MaxDistanceKm = null,
    string? Query = null,
    int Page = 1,
    int PageSize = 24
) : IRequest<SearchResultsDto>;

public class SearchServicesQueryHandler(ISearchRepository search)
    : IRequestHandler<SearchServicesQuery, SearchResultsDto>
{
    public async Task<SearchResultsDto> Handle(SearchServicesQuery request, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3)); // Baghdad is UTC+3
        var targetDate = request.Availability == AvailabilityWindow.Today
            ? today
            : request.Date ?? today;

        // How far ahead to look when the customer hasn't pinned a single date.
        var windowEnd = request.Availability switch
        {
            AvailabilityWindow.ThisWeek => targetDate.AddDays(6),
            AvailabilityWindow.Any => targetDate.AddDays(30),
            _ => targetDate
        };

        var mealTypes = (request.MealTypes ?? [])
            .Select(m => Enum.TryParse<MealType>(m, true, out var parsed) ? parsed : (MealType?)null)
            .Where(m => m.HasValue).Select(m => m!.Value).ToArray();

        var candidates = await search.FindCandidatesAsync(new ServiceSearchFilter(
            request.Type,
            request.CitySlug,
            request.AreaId,
            request.MinPrice,
            request.MaxPrice,
            FlagEnums.Combine<Cuisines>(request.Cuisines),
            FlagEnums.Combine<DietaryTags>(request.Dietary),
            FlagEnums.Combine<RestaurantFeatures>(request.Features),
            mealTypes,
            request.BookingMode,
            request.Guests,
            request.Query), ct);

        if (candidates.Count == 0)
        {
            return new SearchResultsDto(0, request.Page, request.PageSize, []);
        }

        var serviceIds = candidates.Select(s => s.Id).ToList();
        var restaurantIds = candidates.Select(s => s.RestaurantId).Distinct().ToList();

        var dayStatuses = await search.GetDayStatusesAsync(serviceIds, targetDate, windowEnd, ct);
        var ratings = await search.GetRatingsAsync(restaurantIds, ct);
        var settings = await search.GetSettingsAsync(restaurantIds, ct);
        var recentBookings = await search.GetRecentBookingCountsAsync(serviceIds, today.AddDays(-30), ct);

        var guests = Math.Max(1, request.Guests ?? 1);
        var rows = new List<(ServiceCardDto Card, Service Service, double? Distance, int Featured)>();

        // Resolve each candidate against a concrete date first, then price its availability
        // on that date — the two batched lookups below need the dates to be known.
        var resolved = new List<(Service Service, DateOnly Date)>();
        foreach (var service in candidates)
        {
            var date = FirstServingDate(service, targetDate, windowEnd, dayStatuses);
            if (date is null) continue;
            resolved.Add((service, date.Value));
        }

        if (resolved.Count == 0)
        {
            return new SearchResultsDto(0, request.Page, request.PageSize, []);
        }

        var overrides = await search.GetSlotOverridesAsync(serviceIds, targetDate, windowEnd, ct);

        // Bookings are per date; group the resolved services by their date so a search that
        // spans a week still costs one query per distinct date rather than one per service.
        var bookedByDate = new Dictionary<DateOnly, Dictionary<(int ServiceId, int? TimeSlotId), int>>();
        foreach (var date in resolved.Select(r => r.Date).Distinct())
        {
            bookedByDate[date] = await search.GetBookedGuestsAsync(
                resolved.Where(r => r.Date == date).Select(r => r.Service.Id).ToList(), date, ct);
        }

        foreach (var (service, date) in resolved)
        {
            var restaurant = service.Restaurant!;
            var tolerance = settings.TryGetValue(service.RestaurantId, out var st) ? st.OverbookingTolerancePercent : 0;

            var slots = AvailabilityCalculator.Build(service, date, bookedByDate[date], overrides, tolerance);
            var openSlots = AvailabilityCalculator.SlotsFor(slots, guests, request.Time);

            if (request.TimeOfDay.HasValue)
            {
                var (from, to) = BandFor(request.TimeOfDay.Value);
                openSlots = openSlots.Where(s => InBand(s.StartTime, from, to)).ToList();
                // A time-of-day filter is a hard filter: no sitting in that band means the
                // service doesn't belong in these results at all.
                if (slots.Count > 0 && !slots.Any(s => InBand(s.StartTime, from, to))) continue;
            }

            ratings.TryGetValue(service.RestaurantId, out var rating);
            var hasRating = ratings.ContainsKey(service.RestaurantId);
            if (request.MinRating.HasValue && (!hasRating || rating.Average < request.MinRating.Value)) continue;

            double? distance = null;
            if (request.Lat.HasValue && request.Lng.HasValue && restaurant.Latitude.HasValue && restaurant.Longitude.HasValue)
            {
                distance = Haversine(request.Lat.Value, request.Lng.Value, restaurant.Latitude.Value, restaurant.Longitude.Value);
                if (request.MaxDistanceKm.HasValue && distance > request.MaxDistanceKm.Value) continue;
            }
            else if (request.MaxDistanceKm.HasValue)
            {
                continue; // asked for nearby, but this restaurant has no coordinates to judge by
            }

            var bookingEnabled = slots.Count > 0;
            var isAvailable = openSlots.Count > 0;
            var spotsLeft = slots.Count > 0 ? slots.Max(s => s.Remaining) : (int?)null;
            var nextTime = openSlots.FirstOrDefault()?.StartTime.ToString("HH:mm");

            recentBookings.TryGetValue(service.Id, out var recent);

            var card = new ServiceCardDto(
                service.Id,
                service.ServiceType,
                service.Name,
                service.NameAr,
                service.Description,
                service.DescriptionAr,
                restaurant.Id,
                restaurant.Name,
                restaurant.NameAr,
                restaurant.Area!.NameEn,
                restaurant.Area!.NameAr,
                restaurant.Area!.City!.NameEn,
                restaurant.Area!.City!.NameAr,
                restaurant.Area!.City!.Slug,
                restaurant.Latitude,
                restaurant.Longitude,
                service.Photos.OrderBy(p => p.SortOrder).Select(p => p.Url).FirstOrDefault() ?? restaurant.CoverPhotoUrl,
                service.MealType,
                FlagEnums.Cuisines(service.Cuisines),
                FlagEnums.Dietary(service.Dietary),
                service.PricingModel,
                PriceCalculator.HeadlinePrice(service),
                service.PricePerChild,
                service.PackageGuests,
                restaurant.Area!.City!.Country?.CurrencyCode ?? "IQD",
                hasRating ? Math.Round(rating.Average, 1) : null,
                hasRating ? rating.Count : 0,
                service.OpensAt.ToString("HH:mm"),
                service.ClosesAt.ToString("HH:mm"),
                service.DurationMinutes,
                service.MinGuests,
                service.MaxGuests,
                isAvailable,
                spotsLeft,
                nextTime,
                bookingEnabled,
                service.BookingMode,
                st?.IsFoundingRestaurant ?? false,
                recent);

            rows.Add((card, service, distance, st?.FeaturedScore ?? 0));
        }

        var sorted = Sort(rows, request.Sort);
        var total = sorted.Count;
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 60);

        var items = sorted.Skip((page - 1) * pageSize).Take(pageSize).Select(r => r.Card).ToList();
        return new SearchResultsDto(total, page, pageSize, items);
    }

    /// The first date in the window on which this service actually runs and hasn't been
    /// switched off. Returns null when it doesn't run at all in the window.
    private static DateOnly? FirstServingDate(
        Service service,
        DateOnly from,
        DateOnly to,
        IReadOnlyDictionary<(int ServiceId, DateOnly Date), bool> dayStatuses)
    {
        for (var d = from; d <= to; d = d.AddDays(1))
        {
            if (!RecurrenceEvaluator.MatchesRecurrence(service, d)) continue;
            if (dayStatuses.TryGetValue((service.Id, d), out var isActive) && !isActive) continue;
            return d;
        }
        return null;
    }

    private static List<(ServiceCardDto Card, Service Service, double? Distance, int Featured)> Sort(
        List<(ServiceCardDto Card, Service Service, double? Distance, int Featured)> rows, SearchSort sort) => sort switch
    {
        SearchSort.PriceLowToHigh => rows.OrderBy(r => r.Card.Price).ThenBy(r => r.Card.Name).ToList(),
        SearchSort.PriceHighToLow => rows.OrderByDescending(r => r.Card.Price).ThenBy(r => r.Card.Name).ToList(),
        SearchSort.Rating => rows.OrderByDescending(r => r.Card.Rating ?? -1).ThenByDescending(r => r.Card.ReviewCount).ToList(),
        SearchSort.Distance => rows.OrderBy(r => r.Distance ?? double.MaxValue).ThenBy(r => r.Card.Name).ToList(),
        SearchSort.Popular => rows.OrderByDescending(r => r.Card.RecentBookings).ThenByDescending(r => r.Card.Rating ?? 0).ToList(),

        // Recommended leads with availability — a card you can't book is a dead end — then
        // the platform's own featured score, then guest rating and demand.
        _ => rows
            .OrderByDescending(r => r.Card.IsAvailable)
            .ThenByDescending(r => r.Featured)
            .ThenByDescending(r => r.Card.Rating ?? 0)
            .ThenByDescending(r => r.Card.RecentBookings)
            .ThenBy(r => r.Card.Name)
            .ToList()
    };

    private static (TimeOnly From, TimeOnly To) BandFor(TimeOfDay band) => band switch
    {
        Features.Search.TimeOfDay.Morning => (new TimeOnly(6, 0), new TimeOnly(11, 0)),
        Features.Search.TimeOfDay.Lunch => (new TimeOnly(11, 0), new TimeOnly(15, 0)),
        Features.Search.TimeOfDay.Afternoon => (new TimeOnly(15, 0), new TimeOnly(18, 0)),
        Features.Search.TimeOfDay.Dinner => (new TimeOnly(18, 0), new TimeOnly(22, 0)),
        _ => (new TimeOnly(22, 0), new TimeOnly(6, 0))
    };

    /// Late night wraps past midnight, so the band check has to handle From > To.
    private static bool InBand(TimeOnly time, TimeOnly from, TimeOnly to) =>
        from <= to ? time >= from && time < to : time >= from || time < to;

    private static double Haversine(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return earthRadiusKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
}
