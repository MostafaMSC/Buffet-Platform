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

/// The two things a restaurant can sell on this platform. A restaurant may offer either,
/// or both, and can have many of each.
public enum ServiceType
{
    Buffet,
    SetMenu
}

public enum MealType
{
    Breakfast,
    Lunch,
    Iftar,
    Sohor,
    Dinner
}

/// How a service is priced. PerPerson multiplies by head count (with an optional separate
/// child price); PerPackage is a flat price covering a fixed number of guests, e.g. a
/// "35,000 IQD for 2 people" romantic set menu.
public enum PricingModel
{
    PerPerson,
    PerPackage
}

public enum ServiceStatus
{
    Draft,
    Active,
    Paused
}

/// Whether a booking is confirmed immediately or held for the restaurant to accept.
public enum BookingMode
{
    Instant,
    Request
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

/// Multi-select cuisine tags on a service. Stored as flags so search can filter with a
/// single bitwise comparison that translates to SQL.
[Flags]
public enum Cuisines
{
    None = 0,
    Iraqi = 1,
    Arabic = 2,
    International = 4,
    Italian = 8,
    Asian = 16,
    Seafood = 32,
    Grill = 64,
    Indian = 128,
    Turkish = 256,
    Lebanese = 512
}

[Flags]
public enum DietaryTags
{
    None = 0,
    Vegetarian = 1,
    Vegan = 2,
    Halal = 4,
    GlutenFree = 8,
    DairyFree = 16,
    NutFree = 32
}

/// Restaurant-level facilities shown on cards and filterable in search.
[Flags]
public enum RestaurantFeatures
{
    None = 0,
    FamilyFriendly = 1,
    PrivateDining = 2,
    OutdoorSeating = 4,
    Parking = 8,
    KidsArea = 16,
    PrivateRoom = 32,
    WheelchairAccessible = 64,
    Shisha = 128
}

public enum BookingStatus
{
    Confirmed,
    Waitlisted,
    Cancelled,
    NoShow,
    Completed,

    /// Awaiting restaurant acceptance — only used by services with BookingMode.Request.
    Pending,

    /// Guest has arrived and been seated.
    CheckedIn,

    /// The restaurant declined a Pending request.
    Rejected
}

public enum WaitlistStatus
{
    Waiting,
    Offered,
    Expired,
    Converted
}
