namespace BuffetDiscovery.Application.Common;

public static class CapacityCalculator
{
    /// Stated capacity plus the restaurant's overbooking tolerance, rounded down.
    public static int EffectiveCapacity(int capacity, int overbookingTolerancePercent) =>
        capacity + capacity * overbookingTolerancePercent / 100;
}
