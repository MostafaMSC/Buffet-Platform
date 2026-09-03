using BuffetDiscovery.Domain.Entities;

namespace BuffetDiscovery.Application.Common.Dtos;

public record TimeSlotDto(int Id, string StartTime, string EndTime, int Capacity, int BufferMinutes);

public record ServiceCapacityDto(int ServiceId, int? Capacity, List<TimeSlotDto> Slots);

public record SlotAvailabilityDto(
    int? TimeSlotId,
    string StartTime,
    string EndTime,
    int Capacity,
    int Booked,
    int Remaining,
    bool IsFull,
    int WaitlistLength
);

public record BookingAvailabilityDto(int ServiceId, DateOnly Date, bool BookingEnabled, List<SlotAvailabilityDto> Slots);

public record BookingDetailDto(
    int Id,
    string ConfirmationCode,
    int RestaurantId,
    string RestaurantName,
    string RestaurantNameAr,
    string RestaurantPhone,
    string AreaName,
    string AreaNameAr,
    string CityName,
    string CityNameAr,
    int ServiceId,
    ServiceType ServiceType,
    string ServiceName,
    string ServiceNameAr,
    MealType MealType,
    string? PhotoUrl,
    DateOnly Date,
    string? SlotStartTime,
    string? SlotEndTime,
    string CustomerName,
    string CustomerPhone,
    string? CustomerEmail,
    string? SpecialRequests,
    int PartySize,
    int Adults,
    int Children,
    decimal TotalPrice,
    string CurrencyCode,
    BookingStatus Status,
    int CancellationCutoffMinutes,
    DateTime CreatedAt
);

public record WaitlistDetailDto(
    int Id,
    int RestaurantId,
    string RestaurantName,
    string RestaurantNameAr,
    int ServiceId,
    MealType MealType,
    DateOnly Date,
    string? SlotStartTime,
    string? SlotEndTime,
    string CustomerName,
    string CustomerPhone,
    int PartySize,
    int Position,
    WaitlistStatus Status,
    DateTime? NotifiedAt,
    int OfferWindowMinutes
);

public record MyLookupResultDto(List<BookingDetailDto> Bookings, List<WaitlistDetailDto> WaitlistEntries);

public record RestaurantSettingsDto(
    int CancellationCutoffMinutes,
    int WaitlistOfferWindowMinutes,
    int OverbookingTolerancePercent,
    bool IsFoundingRestaurant,
    int? ReferredByRestaurantId,
    int FeaturedScore
);

public record RestaurantBookingListItemDto(
    int Id,
    string ConfirmationCode,
    string CustomerName,
    string CustomerPhone,
    string? CustomerEmail,
    string? SpecialRequests,
    int PartySize,
    int Adults,
    int Children,
    decimal TotalPrice,
    BookingStatus Status,
    DateTime CreatedAt
);

public record RestaurantBookingGroupDto(
    int ServiceId,
    string ServiceName,
    string ServiceNameAr,
    ServiceType ServiceType,
    MealType MealType,
    DateOnly Date,
    int? TimeSlotId,
    string StartTime,
    string EndTime,
    int Capacity,
    int EffectiveCapacity,
    int BookedPartySize,
    List<RestaurantBookingListItemDto> Bookings
);

/// The numbers the restaurant sees first thing each day.
public record DashboardOverviewDto(
    DateOnly Date,
    int TodayBookings,
    int TodayGuests,
    int PendingRequests,
    int UpcomingBookings,
    int UpcomingGuests,
    decimal TodayRevenue,
    decimal Revenue30Days,
    int BuffetBookings30Days,
    int SetMenuBookings30Days,
    double NoShowRatePercent,
    double CancellationRatePercent,
    string? TopServiceName,
    string? TopServiceNameAr,
    int TopServiceBookings,
    List<DailyBookingStatDto> Last14Days
);

public record DailyBookingStatDto(DateOnly Date, int TotalPartySize, int BookingCount);

public record SlotBookingStatDto(int? TimeSlotId, string Label, int TotalPartySize, int BookingCount);

public record BookingAnalyticsDto(
    int TotalBookings,
    int CompletedCount,
    int NoShowCount,
    int CancelledCount,
    double NoShowRatePercent,
    List<DailyBookingStatDto> ByDate,
    List<SlotBookingStatDto> BySlot
);

public record PlatformBookingStatsDto(
    int TotalBookings,
    int TotalPartySize,
    int RestaurantsWithBookings,
    List<DailyBookingStatDto> ByDate
);

public record AdminRestaurantSettingsDto(
    int RestaurantId,
    string RestaurantName,
    int CancellationCutoffMinutes,
    int OverbookingTolerancePercent,
    bool IsFoundingRestaurant,
    int FeaturedScore,
    int? ReferredByRestaurantId,
    string? ReferredByName
);
