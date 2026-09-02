using BuffetDiscovery.Domain.Entities;

namespace BuffetDiscovery.Application.Common;

public static class WeekdayMapper
{
    public static WeekDays ToFlags(List<string>? days)
    {
        if (days is null) return WeekDays.None;
        var result = WeekDays.None;
        foreach (var d in days)
        {
            if (Enum.TryParse<WeekDays>(d, true, out var parsed))
            {
                result |= parsed;
            }
        }
        return result;
    }

    public static List<string> ToList(WeekDays w) =>
        Enum.GetValues<WeekDays>()
            .Where(v => v != WeekDays.None && w.HasFlag(v))
            .Select(v => v.ToString())
            .ToList();
}
