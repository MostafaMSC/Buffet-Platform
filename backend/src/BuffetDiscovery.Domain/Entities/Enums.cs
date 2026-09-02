namespace BuffetDiscovery.Domain.Entities;

public enum RestaurantStatus
{
    Pending,
    Approved,
    Suspended,
    Rejected
}

public enum UserRole
{
    RestaurantOwner,
    Admin
}

public enum MealType
{
    Breakfast,
    Lunch,
    Iftar,
    Sohor
}

public enum RecurrenceType
{
    Daily,
    SpecificWeekdays,
    RamadanMode,
    OneOff
}

[Flags]
public enum WeekDays
{
    None = 0,
    Monday = 1,
    Tuesday = 2,
    Wednesday = 4,
    Thursday = 8,
    Friday = 16,
    Saturday = 32,
    Sunday = 64
}
