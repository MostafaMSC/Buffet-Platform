using BuffetDiscovery.Domain.Entities;

namespace BuffetDiscovery.Application.Common;

/// What a slot looks like on one date once bookings, overrides and overbooking tolerance
/// are applied.
public record SlotAvailability(
    int? TimeSlotId,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int Capacity,
    int EffectiveCapacity,
    int Booked,
    bool IsBlocked)
{
    /// A sitting the restaurant has closed for the date has no seats to give, whatever its
    /// configured capacity says — otherwise callers would advertise seats they cannot sell.
    public int Remaining => IsBlocked ? 0 : Math.Max(0, EffectiveCapacity - Booked);
    public bool IsFull => IsBlocked || Remaining <= 0;
    public bool Fits(int guests) => !IsBlocked && Remaining >= guests;
}

/// The single source of truth for "can this party sit here on this date". Every surface —
/// search results, the detail page, the booking call and the restaurant calendar — goes
/// through here, so none of them can disagree about a service's remaining seats.
public static class AvailabilityCalculator
{
    /// Builds the slot picture for one service on one date. A service split into time slots
    /// yields one entry per slot; a whole-window service yields a single entry covering its
    /// opening hours. A service with no capacity configured yields nothing — it isn't
    /// taking bookings at all.
    public static List<SlotAvailability> Build(
        Service service,
        DateOnly date,
        IReadOnlyDictionary<(int ServiceId, int? TimeSlotId), int> bookedBySlot,
        IReadOnlyDictionary<(int TimeSlotId, DateOnly Date), SlotOverride> overrides,
        int overbookingTolerancePercent)
    {
        var result = new List<SlotAvailability>();
        var slots = service.TimeSlots.Where(s => !s.IsDeleted).OrderBy(s => s.StartTime).ToList();

        if (slots.Count > 0)
        {
            foreach (var slot in slots)
            {
                overrides.TryGetValue((slot.Id, date), out var ov);
                var capacity = ov?.Capacity ?? slot.Capacity;
                bookedBySlot.TryGetValue((service.Id, slot.Id), out var booked);

                result.Add(new SlotAvailability(
                    slot.Id,
                    slot.StartTime,
                    slot.EndTime,
                    capacity,
                    CapacityCalculator.EffectiveCapacity(capacity, overbookingTolerancePercent),
                    booked,
                    ov?.IsBlocked ?? false));
            }

            return result;
        }

        if (service.Capacity.HasValue)
        {
            bookedBySlot.TryGetValue((service.Id, null), out var booked);
            result.Add(new SlotAvailability(
                null,
                service.OpensAt,
                service.ClosesAt,
                service.Capacity.Value,
                CapacityCalculator.EffectiveCapacity(service.Capacity.Value, overbookingTolerancePercent),
                booked,
                false));
        }

        return result;
    }

    /// Slots that could seat this party, optionally narrowed to a requested time. A slot
    /// covers a time when the sitting starts at or before it and ends after it.
    public static List<SlotAvailability> SlotsFor(
        IEnumerable<SlotAvailability> slots,
        int guests,
        TimeOnly? atTime) =>
        slots
            .Where(s => atTime is null || (s.StartTime <= atTime.Value && s.EndTime > atTime.Value))
            .Where(s => s.Fits(guests))
            .ToList();

    /// A service's booking window has passed for the day once its last sitting has started.
    public static bool IsPast(SlotAvailability slot, DateOnly date, DateTime nowLocal, int minAdvanceMinutes) =>
        date.ToDateTime(slot.StartTime).AddMinutes(-minAdvanceMinutes) <= nowLocal;
}
