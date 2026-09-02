using BuffetDiscovery.Domain.Entities;

namespace BuffetDiscovery.Domain.Services;

/// Pure domain logic: whether a BuffetOffering's recurrence rule says "on" for a
/// given date, ignoring any explicit per-date AvailabilityStatus override. Has no
/// dependency on persistence or any other layer.
public static class RecurrenceEvaluator
{
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
}
