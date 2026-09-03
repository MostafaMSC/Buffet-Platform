export type MealType = 'Breakfast' | 'Lunch' | 'Iftar' | 'Sohor'
export type RecurrenceType = 'Daily' | 'SpecificWeekdays' | 'RamadanMode' | 'OneOff'
export type RestaurantStatus = 'Pending' | 'Approved' | 'Suspended' | 'Rejected'
export type WeekdayName =
  | 'Monday'
  | 'Tuesday'
  | 'Wednesday'
  | 'Thursday'
  | 'Friday'
  | 'Saturday'
  | 'Sunday'

export interface Area {
  id: number
  nameEn: string
  nameAr: string
}

export interface OfferingListItem {
  offeringId: number
  restaurantId: number
  restaurantName: string
  restaurantNameAr: string
  areaId: number
  areaNameEn: string
  areaNameAr: string
  coverPhotoUrl: string | null
  mealType: MealType
  price: number
  opensAt: string
  closesAt: string
  isFoundingRestaurant: boolean
}

export interface RestaurantOffering {
  id: number
  mealType: MealType
  price: number
  opensAt: string
  closesAt: string
  description: string | null
  descriptionAr: string | null
  photoUrls: string[]
  videoUrl: string | null
  isActiveToday: boolean
}

export interface RestaurantDetail {
  id: number
  name: string
  nameAr: string
  areaNameEn: string
  areaNameAr: string
  phoneNumber: string
  address: string | null
  googleMapsUrl: string | null
  description: string | null
  descriptionAr: string | null
  logoUrl: string | null
  coverPhotoUrl: string | null
  isFoundingRestaurant: boolean
  offerings: RestaurantOffering[]
}

export interface AuthResponse {
  token: string
  role: 'RestaurantOwner' | 'Admin'
  restaurantId: number | null
  restaurantStatus: RestaurantStatus | null
}

export interface RestaurantProfile {
  id: number
  name: string
  nameAr: string
  areaId: number
  areaNameEn: string
  phoneNumber: string
  address: string | null
  googleMapsUrl: string | null
  description: string | null
  descriptionAr: string | null
  logoUrl: string | null
  coverPhotoUrl: string | null
  status: RestaurantStatus
}

export interface DayStatus {
  date: string
  isActive: boolean
}

export interface DashboardOffering {
  id: number
  mealType: MealType
  price: number
  opensAt: string
  closesAt: string
  description: string | null
  descriptionAr: string | null
  recurrence: RecurrenceType
  weekdays: WeekdayName[]
  ramadanStartDate: string | null
  ramadanEndDate: string | null
  oneOffDate: string | null
  photoUrls: string[]
  videoUrl: string | null
  days: DayStatus[]
}

export interface OfferingInput {
  mealType: MealType
  price: number
  opensAt: string
  closesAt: string
  description?: string | null
  descriptionAr?: string | null
  recurrence: RecurrenceType
  weekdays?: WeekdayName[] | null
  ramadanStartDate?: string | null
  ramadanEndDate?: string | null
  oneOffDate?: string | null
  photoUrls?: string[] | null
  videoUrl?: string | null
}

export interface RestaurantAdminListItem {
  id: number
  name: string
  nameAr: string
  areaNameEn: string
  phoneNumber: string
  status: RestaurantStatus
  createdAt: string
  offeringCount: number
}

// ---------- Booking (Phase 2) ----------

export type BookingStatus = 'Confirmed' | 'Waitlisted' | 'Cancelled' | 'NoShow' | 'Completed'
export type WaitlistStatus = 'Waiting' | 'Offered' | 'Expired' | 'Converted'

export interface TimeSlot {
  id: number
  startTime: string
  endTime: string
  capacity: number
  bufferMinutes: number
}

export interface OfferingCapacity {
  offeringId: number
  capacity: number | null
  slots: TimeSlot[]
}

export interface RestaurantSettings {
  cancellationCutoffMinutes: number
  waitlistOfferWindowMinutes: number
  overbookingTolerancePercent: number
  isFoundingRestaurant: boolean
  referredByRestaurantId: number | null
  featuredScore: number
}

export interface SlotAvailability {
  timeSlotId: number | null
  startTime: string
  endTime: string
  capacity: number
  booked: number
  remaining: number
  isFull: boolean
  waitlistLength: number
}

export interface BookingAvailability {
  offeringId: number
  date: string
  bookingEnabled: boolean
  slots: SlotAvailability[]
}

export interface BookingDetail {
  id: number
  confirmationCode: string
  restaurantId: number
  restaurantName: string
  restaurantNameAr: string
  offeringId: number
  mealType: MealType
  date: string
  slotStartTime: string | null
  slotEndTime: string | null
  customerName: string
  customerPhone: string
  partySize: number
  status: BookingStatus
  createdAt: string
}

export interface WaitlistDetail {
  id: number
  restaurantId: number
  restaurantName: string
  restaurantNameAr: string
  offeringId: number
  mealType: MealType
  date: string
  slotStartTime: string | null
  slotEndTime: string | null
  customerName: string
  customerPhone: string
  partySize: number
  position: number
  status: WaitlistStatus
  notifiedAt: string | null
  offerWindowMinutes: number
}

export interface MyLookupResult {
  bookings: BookingDetail[]
  waitlistEntries: WaitlistDetail[]
}

export interface RestaurantBookingListItem {
  id: number
  confirmationCode: string
  customerName: string
  customerPhone: string
  partySize: number
  status: BookingStatus
  createdAt: string
}

export interface RestaurantBookingGroup {
  offeringId: number
  mealType: MealType
  date: string
  timeSlotId: number | null
  startTime: string
  endTime: string
  capacity: number
  effectiveCapacity: number
  bookedPartySize: number
  bookings: RestaurantBookingListItem[]
}

export interface DailyBookingStat {
  date: string
  totalPartySize: number
  bookingCount: number
}

export interface SlotBookingStat {
  timeSlotId: number | null
  label: string
  totalPartySize: number
  bookingCount: number
}

export interface BookingAnalytics {
  totalBookings: number
  completedCount: number
  noShowCount: number
  cancelledCount: number
  noShowRatePercent: number
  byDate: DailyBookingStat[]
  bySlot: SlotBookingStat[]
}

export interface PlatformBookingStats {
  totalBookings: number
  totalPartySize: number
  restaurantsWithBookings: number
  byDate: DailyBookingStat[]
}

export interface AdminRestaurantSettings {
  restaurantId: number
  restaurantName: string
  cancellationCutoffMinutes: number
  overbookingTolerancePercent: number
  isFoundingRestaurant: boolean
  featuredScore: number
  referredByRestaurantId: number | null
  referredByName: string | null
}
