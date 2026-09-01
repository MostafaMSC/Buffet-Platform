using BuffetDiscovery.Api.Data;
using BuffetDiscovery.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuffetDiscovery.Api.Services;

public class AvailabilityService(AppDbContext db)
{
    /// Whether an offering's recurrence rule says "on" for a given date,
    /// ignoring any explicit per-date override that might exist.
    public static bool MatchesRecurrence(BuffetOffering offering, DateOnly date)
    {
        return offering.Recurrence switch
        {
            RecurrenceType.Daily => true,
            RecurrenceType.SpecificWeekdays => offering.Weekdays.HasFlag(ToWeekDay(date.DayOfWeek)),
            RecurrenceType.RamadanMode => offering.RamadanStartDate.HasValue
                && offering.RamadanEndDate.HasValue
                && date >= offering.RamadanStartDate.Value
                && date <= offering.RamadanEndDate.Value,
            RecurrenceType.OneOff => offering.OneOffDate.HasValue && offering.OneOffDate.Value == date,
            _ => false
        };
    }

    private static WeekDays ToWeekDay(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => WeekDays.Monday,
        DayOfWeek.Tuesday => WeekDays.Tuesday,
        DayOfWeek.Wednesday => WeekDays.Wednesday,
        DayOfWeek.Thursday => WeekDays.Thursday,
        DayOfWeek.Friday => WeekDays.Friday,
        DayOfWeek.Saturday => WeekDays.Saturday,
        DayOfWeek.Sunday => WeekDays.Sunday,
        _ => WeekDays.None
    };

    /// Returns the effective is_active status for offering+date, creating a
    /// concrete AvailabilityStatus row from the recurrence default if one
    /// doesn't exist yet. Call SaveChanges after use if new rows were tracked.
    public async Task<bool> GetOrMaterializeAsync(BuffetOffering offering, DateOnly date, CancellationToken ct = default)
    {
        var existing = await db.AvailabilityStatuses
            .FirstOrDefaultAsync(a => a.OfferingId == offering.Id && a.Date == date, ct);

        if (existing is not null)
        {
            return existing.IsActive;
        }

        var defaultActive = MatchesRecurrence(offering, date);

        db.AvailabilityStatuses.Add(new AvailabilityStatus
        {
            OfferingId = offering.Id,
            Date = date,
            IsActive = defaultActive
        });

        return defaultActive;
    }

    /// Ensures AvailabilityStatus rows exist for the offering across a date range.
    public async Task MaterializeRangeAsync(BuffetOffering offering, DateOnly start, DateOnly end, CancellationToken ct = default)
    {
        var existingDates = await db.AvailabilityStatuses
            .Where(a => a.OfferingId == offering.Id && a.Date >= start && a.Date <= end)
            .Select(a => a.Date)
            .ToListAsync(ct);

        var existingSet = existingDates.ToHashSet();

        for (var date = start; date <= end; date = date.AddDays(1))
        {
            if (existingSet.Contains(date)) continue;

            db.AvailabilityStatuses.Add(new AvailabilityStatus
            {
                OfferingId = offering.Id,
                Date = date,
                IsActive = MatchesRecurrence(offering, date)
            });
        }
    }
}
