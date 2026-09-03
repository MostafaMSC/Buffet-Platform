using BuffetDiscovery.Domain.Entities;

namespace BuffetDiscovery.Domain.Services;

/// Pure domain logic: whether a Service's recurrence rule says "on" for a
/// given date, ignoring any explicit per-date AvailabilityStatus override. Has no
/// dependency on persistence or any other layer.
public static class RecurrenceEvaluator
{
    public static bool MatchesRecurrence(Service service, DateOnly date)
    {
        return service.Recurrence switch
        {
            RecurrenceType.Daily => true,
            RecurrenceType.SpecificWeekdays => service.Weekdays.HasFlag(ToWeekDay(date.DayOfWeek)),
            RecurrenceType.RamadanMode => service.RamadanStartDate.HasValue
                && service.RamadanEndDate.HasValue
                && date >= service.RamadanStartDate.Value
                && date <= service.RamadanEndDate.Value,
            RecurrenceType.OneOff => service.OneOffDate.HasValue && service.OneOffDate.Value == date,
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
