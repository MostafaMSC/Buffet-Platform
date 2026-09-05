using BuffetDiscovery.Domain.Entities;

namespace BuffetDiscovery.Application.Common;

/// Flags enums are compact to store and cheap to filter on in SQL, but a comma-joined
/// string is awkward for a client to render. These turn them into plain string arrays at
/// the API edge, and back again when a client sends a selection.
public static class FlagEnums
{
    public static string[] Split<TEnum>(TEnum value) where TEnum : struct, Enum =>
        Enum.GetValues<TEnum>()
            .Where(v => Convert.ToInt32(v) != 0 && (Convert.ToInt32(value) & Convert.ToInt32(v)) == Convert.ToInt32(v))
            .Select(v => v.ToString())
            .ToArray();

    public static TEnum Combine<TEnum>(IEnumerable<string>? names) where TEnum : struct, Enum
    {
        var combined = 0;
        foreach (var name in names ?? [])
        {
            if (Enum.TryParse<TEnum>(name, ignoreCase: true, out var parsed))
            {
                combined |= Convert.ToInt32(parsed);
            }
        }
        return (TEnum)Enum.ToObject(typeof(TEnum), combined);
    }

    public static string[] Cuisines(Cuisines value) => Split(value);
    public static string[] Dietary(DietaryTags value) => Split(value);
    public static string[] Features(RestaurantFeatures value) => Split(value);
}
