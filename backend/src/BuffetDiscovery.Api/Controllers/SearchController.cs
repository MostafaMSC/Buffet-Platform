using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Features.Search;
using BuffetDiscovery.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BuffetDiscovery.Api.Controllers;

/// Public discovery surface: one search endpoint answering every filter the UI offers, the
/// homepage feed, and the location tree that drives the "where" picker.
[ApiController]
[Route("api")]
public class SearchController(ISender mediator) : ControllerBase
{
    [HttpGet("search")]
    public async Task<ActionResult<SearchResultsDto>> Search(
        [FromQuery] ServiceType? type,
        [FromQuery] string? city,
        [FromQuery] int? areaId,
        [FromQuery] DateOnly? date,
        [FromQuery] TimeOnly? time,
        [FromQuery] TimeOfDay? timeOfDay,
        [FromQuery] int? guests,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] string[]? cuisines,
        [FromQuery] string[]? dietary,
        [FromQuery] string[]? features,
        [FromQuery] string[]? mealTypes,
        [FromQuery] BookingMode? bookingMode,
        [FromQuery] double? minRating,
        [FromQuery] AvailabilityWindow availability = AvailabilityWindow.SelectedDate,
        [FromQuery] SearchSort sort = SearchSort.Recommended,
        [FromQuery] double? lat = null,
        [FromQuery] double? lng = null,
        [FromQuery] double? maxDistanceKm = null,
        [FromQuery] string? q = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken ct = default)
    {
        return Ok(await mediator.Send(new SearchServicesQuery(
            type, city, areaId, date, time, timeOfDay, guests, minPrice, maxPrice,
            cuisines, dietary, features, mealTypes, bookingMode, minRating,
            availability, sort, lat, lng, maxDistanceKm, q, page, pageSize), ct));
    }

    [HttpGet("home")]
    public async Task<ActionResult<HomeFeedDto>> Home([FromQuery] string? city, CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetHomeFeedQuery(city), ct));
    }

    [HttpGet("locations")]
    public async Task<ActionResult<List<CountryOptionDto>>> Locations(CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetLocationsQuery(), ct));
    }
}
