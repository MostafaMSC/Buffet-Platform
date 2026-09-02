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
