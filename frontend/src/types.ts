// ---------------------------------------------------------------- enums

export type ServiceType = 'Buffet' | 'SetMenu'
export type MealType = 'Breakfast' | 'Lunch' | 'Iftar' | 'Sohor' | 'Dinner'
export type PricingModel = 'PerPerson' | 'PerPackage'
export type ServiceStatus = 'Draft' | 'Active' | 'Paused'
export type BookingMode = 'Instant' | 'Request'
export type RecurrenceType = 'Daily' | 'SpecificWeekdays' | 'RamadanMode' | 'OneOff'
export type RestaurantStatus = 'Pending' | 'Approved' | 'Suspended' | 'Rejected'

export type BookingStatus =
  | 'Confirmed'
  | 'Waitlisted'
  | 'Cancelled'
  | 'NoShow'
  | 'Completed'
  | 'Pending'
  | 'CheckedIn'
  | 'Rejected'

export type WaitlistStatus = 'Waiting' | 'Offered' | 'Expired' | 'Converted'

export type WeekdayName =
  | 'Monday' | 'Tuesday' | 'Wednesday' | 'Thursday' | 'Friday' | 'Saturday' | 'Sunday'

export const CUISINES = [
  'Iraqi', 'Arabic', 'International', 'Italian', 'Asian',
  'Seafood', 'Grill', 'Indian', 'Turkish', 'Lebanese',
] as const
export type Cuisine = (typeof CUISINES)[number]

export const DIETARY_TAGS = [
  'Vegetarian', 'Vegan', 'Halal', 'GlutenFree', 'DairyFree', 'NutFree',
] as const
export type DietaryTag = (typeof DIETARY_TAGS)[number]

export const RESTAURANT_FEATURES = [
  'FamilyFriendly', 'PrivateDining', 'OutdoorSeating', 'Parking',
  'KidsArea', 'PrivateRoom', 'WheelchairAccessible', 'Shisha',
] as const
export type RestaurantFeature = (typeof RESTAURANT_FEATURES)[number]

export const MEAL_TYPES: MealType[] = ['Breakfast', 'Lunch', 'Dinner', 'Iftar', 'Sohor']

export type TimeOfDay = 'Morning' | 'Lunch' | 'Afternoon' | 'Dinner' | 'LateNight'
export type SearchSort = 'Recommended' | 'PriceLowToHigh' | 'PriceHighToLow' | 'Rating' | 'Distance' | 'Popular'
export type AvailabilityWindow = 'SelectedDate' | 'Today' | 'ThisWeek' | 'Any'

// ---------------------------------------------------------------- locations

export interface AreaOption {
  id: number
  nameEn: string
  nameAr: string
  slug: string
}

export interface CityOption {
  id: number
  nameEn: string
  nameAr: string
  slug: string
  areas: AreaOption[]
}

export interface CountryOption {
  id: number
  nameEn: string
  nameAr: string
  code: string
  currencyCode: string
  cities: CityOption[]
}

export interface CityCard {
  id: number
  slug: string
  nameEn: string
  nameAr: string
  imageUrl: string | null
  serviceCount: number
}

// ---------------------------------------------------------------- search

export interface ServiceCard {
  serviceId: number
  serviceType: ServiceType
  name: string
  nameAr: string
  description: string | null
  descriptionAr: string | null

  restaurantId: number
  restaurantName: string
  restaurantNameAr: string
  areaName: string
  areaNameAr: string
  cityName: string
  cityNameAr: string
  citySlug: string
  latitude: number | null
  longitude: number | null

  photoUrl: string | null
  mealType: MealType
  cuisines: Cuisine[]
  dietary: DietaryTag[]

  pricingModel: PricingModel
  price: number
  priceChild: number | null
  packageGuests: number | null
  currencyCode: string

  rating: number | null
  reviewCount: number

  opensAt: string
  closesAt: string
  durationMinutes: number | null
  minGuests: number
  maxGuests: number | null

  isAvailable: boolean
  spotsLeft: number | null
  nextAvailableTime: string | null
  bookingEnabled: boolean
  bookingMode: BookingMode

  isFoundingRestaurant: boolean
  recentBookings: number
}

export interface SearchResults {
  total: number
  page: number
  pageSize: number
  items: ServiceCard[]
}

export interface HomeFeed {
  availableToday: ServiceCard[]
  popularBuffets: ServiceCard[]
  popularSetMenus: ServiceCard[]
  featured: ServiceCard[]
  cities: CityCard[]
}

// ---------------------------------------------------------------- detail

export interface MenuItem {
  id: number
  name: string
  nameAr: string
  description: string | null
  descriptionAr: string | null
  dietary: DietaryTag[]
}

export interface MenuSection {
  id: number
  name: string
  nameAr: string
  items: MenuItem[]
}

export interface Review {
  id: number
  customerName: string
  rating: number
  comment: string | null
  createdAt: string
  isVerified: boolean
}

export interface RestaurantSummary {
  id: number
  name: string
  nameAr: string
  description: string | null
  descriptionAr: string | null
  phoneNumber: string
  address: string | null
  googleMapsUrl: string | null
  latitude: number | null
  longitude: number | null
  logoUrl: string | null
  coverPhotoUrl: string | null
  areaName: string
  areaNameAr: string
  cityName: string
  cityNameAr: string
  citySlug: string
  features: RestaurantFeature[]
  rating: number | null
  reviewCount: number
}

export interface ServiceSlot {
  timeSlotId: number | null
  startTime: string
  endTime: string
  capacity: number
  booked: number
  remaining: number
  isFull: boolean
  fitsParty: boolean
  isPast: boolean
}

export interface ServiceAvailability {
  serviceId: number
  date: string
  isServedOnDate: boolean
  bookingEnabled: boolean
  slots: ServiceSlot[]
}

export interface PriceQuote {
  pricingModel: PricingModel
  adults: number
  children: number
  adultUnitPrice: number
  childUnitPrice: number
  adultsTotal: number
  childrenTotal: number
  packages: number | null
  packagePrice: number | null
  total: number
  currencyCode: string
}

export interface ServiceDetail {
  id: number
  serviceType: ServiceType
  name: string
  nameAr: string
  description: string | null
  descriptionAr: string | null
  mealType: MealType
  cuisines: Cuisine[]
  dietary: DietaryTag[]

  pricingModel: PricingModel
  pricePerAdult: number
  pricePerChild: number | null
  childAgeFrom: number | null
  childAgeTo: number | null
  freeUnderAge: number | null
  packagePrice: number | null
  packageGuests: number | null
  currencyCode: string

  minGuests: number
  maxGuests: number | null
  durationMinutes: number | null
  opensAt: string
  closesAt: string
  recurrence: RecurrenceType
  weekdays: WeekdayName[]
  ramadanStartDate: string | null
  ramadanEndDate: string | null
  oneOffDate: string | null

  bookingMode: BookingMode
  minAdvanceMinutes: number
  cancellationCutoffMinutes: number

  photoUrls: string[]
  videoUrl: string | null
  menu: MenuSection[]

  restaurant: RestaurantSummary
  availability: ServiceAvailability
  quote: PriceQuote
  reviews: Review[]
  similarServices: ServiceCard[]
}

export interface RestaurantPage {
  restaurant: RestaurantSummary
  services: ServiceCard[]
  reviews: Review[]
}

// ---------------------------------------------------------------- bookings

export interface BookingDetail {
  id: number
  confirmationCode: string
  restaurantId: number
  restaurantName: string
  restaurantNameAr: string
  restaurantPhone: string
  areaName: string
  areaNameAr: string
  cityName: string
  cityNameAr: string
  serviceId: number
  serviceType: ServiceType
  serviceName: string
  serviceNameAr: string
  mealType: MealType
  photoUrl: string | null
  date: string
  slotStartTime: string | null
  slotEndTime: string | null
  customerName: string
  customerPhone: string
  customerEmail: string | null
  specialRequests: string | null
  partySize: number
  adults: number
  children: number
  totalPrice: number
  currencyCode: string
  status: BookingStatus
  cancellationCutoffMinutes: number
  createdAt: string
}

export interface WaitlistDetail {
  id: number
  restaurantId: number
  restaurantName: string
  restaurantNameAr: string
  serviceId: number
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

// ---------------------------------------------------------------- restaurant dashboard

export interface DayStatus {
  date: string
  isActive: boolean
}

export interface TimeSlotDto {
  id: number
  startTime: string
  endTime: string
  capacity: number
  bufferMinutes: number
}

export interface DashboardService {
  id: number
  serviceType: ServiceType
  name: string
  nameAr: string
  description: string | null
  descriptionAr: string | null
  mealType: MealType
  status: ServiceStatus
  pricingModel: PricingModel
  pricePerAdult: number
  pricePerChild: number | null
  packagePrice: number | null
  packageGuests: number | null
  minGuests: number
  maxGuests: number | null
  durationMinutes: number | null
  opensAt: string
  closesAt: string
  recurrence: RecurrenceType
  weekdays: WeekdayName[]
  ramadanStartDate: string | null
  ramadanEndDate: string | null
  oneOffDate: string | null
  bookingMode: BookingMode
  capacity: number | null
  slotCount: number
  cuisines: Cuisine[]
  dietary: DietaryTag[]
  photoUrls: string[]
  videoUrl: string | null
  menuSectionCount: number
  days: DayStatus[]
}

export interface ServiceEditor {
  id: number
  serviceType: ServiceType
  name: string
  nameAr: string
  description: string | null
  descriptionAr: string | null
  mealType: MealType
  cuisines: Cuisine[]
  dietary: DietaryTag[]
  status: ServiceStatus
  pricingModel: PricingModel
  pricePerAdult: number
  pricePerChild: number | null
  childAgeFrom: number | null
  childAgeTo: number | null
  freeUnderAge: number | null
  packagePrice: number | null
  packageGuests: number | null
  minGuests: number
  maxGuests: number | null
  durationMinutes: number | null
  opensAt: string
  closesAt: string
  recurrence: RecurrenceType
  weekdays: WeekdayName[]
  ramadanStartDate: string | null
  ramadanEndDate: string | null
  oneOffDate: string | null
  bookingMode: BookingMode
  minAdvanceMinutes: number
  cancellationCutoffMinutes: number | null
  capacity: number | null
  photoUrls: string[]
  videoUrl: string | null
  slots: TimeSlotDto[]
  menu: MenuSection[]
}

/// Payload the service editor sends — mirrors ServiceInput on the API.
export interface ServiceInput {
  serviceType: ServiceType
  name: string
  nameAr: string
  description: string | null
  descriptionAr: string | null
  mealType: MealType
  cuisines: Cuisine[]
  dietary: DietaryTag[]
  status: ServiceStatus
  pricingModel: PricingModel
  pricePerAdult: number
  pricePerChild: number | null
  childAgeFrom: number | null
  childAgeTo: number | null
  freeUnderAge: number | null
  packagePrice: number | null
  packageGuests: number | null
  minGuests: number
  maxGuests: number | null
  durationMinutes: number | null
  opensAt: string
  closesAt: string
  recurrence: RecurrenceType
  weekdays: WeekdayName[]
  ramadanStartDate: string | null
  ramadanEndDate: string | null
  oneOffDate: string | null
  bookingMode: BookingMode
  minAdvanceMinutes: number
  cancellationCutoffMinutes: number | null
  capacity: number | null
  slots: { startTime: string; endTime: string; capacity: number; bufferMinutes: number }[]
  photoUrls: string[]
  videoUrl: string | null
  menu: {
    name: string
    nameAr: string
    items: { name: string; nameAr: string; description: string | null; descriptionAr: string | null; dietary: DietaryTag[] }[]
  }[]
}

export interface RestaurantBookingListItem {
  id: number
  confirmationCode: string
  customerName: string
  customerPhone: string
  customerEmail: string | null
  specialRequests: string | null
  partySize: number
  adults: number
  children: number
  totalPrice: number
  status: BookingStatus
  createdAt: string
}

export interface RestaurantBookingGroup {
  serviceId: number
  serviceName: string
  serviceNameAr: string
  serviceType: ServiceType
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

export interface DashboardOverview {
  date: string
  todayBookings: number
  todayGuests: number
  pendingRequests: number
  upcomingBookings: number
  upcomingGuests: number
  todayRevenue: number
  revenue30Days: number
  buffetBookings30Days: number
  setMenuBookings30Days: number
  noShowRatePercent: number
  cancellationRatePercent: number
  topServiceName: string | null
  topServiceNameAr: string | null
  topServiceBookings: number
  last14Days: DailyBookingStat[]
}

export interface CalendarSlot {
  timeSlotId: number | null
  startTime: string
  endTime: string
  capacity: number
  booked: number
  remaining: number
  isBlocked: boolean
  note: string | null
}

export interface CalendarServiceDay {
  serviceId: number
  serviceName: string
  serviceNameAr: string
  isServed: boolean
  isDayOn: boolean
  slots: CalendarSlot[]
}

export interface CalendarDay {
  date: string
  totalCapacity: number
  totalBooked: number
  services: CalendarServiceDay[]
}

// ---------------------------------------------------------------- account & admin

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

export interface RestaurantAdminListItem {
  id: number
  name: string
  nameAr: string
  areaNameEn: string
  phoneNumber: string
  status: RestaurantStatus
  createdAt: string
  serviceCount: number
}

export interface RestaurantSettings {
  cancellationCutoffMinutes: number
  waitlistOfferWindowMinutes: number
  overbookingTolerancePercent: number
  isFoundingRestaurant: boolean
  referredByRestaurantId: number | null
  featuredScore: number
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

export interface PlatformBookingStats {
  totalBookings: number
  totalPartySize: number
  restaurantsWithBookings: number
  byDate: DailyBookingStat[]
}
