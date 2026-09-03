import { api } from './client'
import type {
  BookingAnalytics,
  BookingDetail,
  CalendarDay,
  CountryOption,
  DashboardOverview,
  DashboardService,
  HomeFeed,
  MyLookupResult,
  RestaurantBookingGroup,
  RestaurantPage,
  SearchResults,
  ServiceAvailability,
  ServiceDetail,
  ServiceEditor,
  ServiceInput,
} from '../types'

/// Every search parameter the UI can set. Undefined values are dropped so the URL and the
/// request stay readable, and repeated values (cuisines, features…) are sent as repeats.
export interface SearchParams {
  type?: string
  city?: string
  areaId?: number
  date?: string
  time?: string
  timeOfDay?: string
  guests?: number
  minPrice?: number
  maxPrice?: number
  cuisines?: string[]
  dietary?: string[]
  features?: string[]
  mealTypes?: string[]
  bookingMode?: string
  minRating?: number
  availability?: string
  sort?: string
  lat?: number
  lng?: number
  maxDistanceKm?: number
  q?: string
  page?: number
  pageSize?: number
}

export function toQuery(params: SearchParams): URLSearchParams {
  const qs = new URLSearchParams()
  for (const [key, value] of Object.entries(params)) {
    if (value === undefined || value === null || value === '') continue
    if (Array.isArray(value)) {
      value.filter(Boolean).forEach((v) => qs.append(key, String(v)))
    } else {
      qs.set(key, String(value))
    }
  }
  return qs
}

// ---------------------------------------------------------------- discovery

export const searchServices = (params: SearchParams, signal?: AbortSignal) =>
  api.get<SearchResults>(`/search?${toQuery(params)}`, { signal }).then((r) => r.data)

export const getHomeFeed = (city?: string) =>
  api.get<HomeFeed>('/home', { params: city ? { city } : undefined }).then((r) => r.data)

export const getLocations = () => api.get<CountryOption[]>('/locations').then((r) => r.data)

export const getServiceDetail = (id: number, date?: string, adults = 2, children = 0) =>
  api.get<ServiceDetail>(`/services/${id}`, { params: { date, adults, children } }).then((r) => r.data)

export const getServiceAvailability = (id: number, date: string, guests: number) =>
  api.get<ServiceAvailability>(`/services/${id}/availability`, { params: { date, guests } }).then((r) => r.data)

export const getRestaurant = (id: number, date?: string) =>
  api.get<RestaurantPage>(`/restaurants/${id}`, { params: { date } }).then((r) => r.data)

// ---------------------------------------------------------------- bookings

export interface CreateBookingPayload {
  serviceId: number
  timeSlotId: number | null
  date: string
  customerName: string
  customerPhone: string
  adults: number
  children: number
  customerEmail?: string | null
  specialRequests?: string | null
}

export const createBooking = (payload: CreateBookingPayload) =>
  api.post<BookingDetail>('/bookings', payload).then((r) => r.data)

export const getBooking = (code: string) =>
  api.get<BookingDetail>(`/bookings/${encodeURIComponent(code)}`).then((r) => r.data)

export const cancelBooking = (code: string) =>
  api.post(`/bookings/${encodeURIComponent(code)}/cancel`).then((r) => r.data)

export const lookupBookings = (phone: string) =>
  api.get<MyLookupResult>('/bookings/mine', { params: { phone } }).then((r) => r.data)

export const joinWaitlist = (payload: {
  serviceId: number
  timeSlotId: number | null
  date: string
  customerName: string
  customerPhone: string
  partySize: number
}) => api.post('/bookings/waitlist', payload).then((r) => r.data)

export const confirmWaitlistOffer = (waitlistId: number, customerPhone: string) =>
  api.post<BookingDetail>(`/bookings/waitlist/${waitlistId}/confirm`, { customerPhone }).then((r) => r.data)

// ---------------------------------------------------------------- restaurant dashboard

export const getDashboardServices = (days = 14) =>
  api.get<DashboardService[]>('/dashboard/services', { params: { days } }).then((r) => r.data)

export const getServiceForEdit = (id: number) =>
  api.get<ServiceEditor>(`/dashboard/services/${id}`).then((r) => r.data)

export const createService = (payload: ServiceInput) =>
  api.post<number>('/dashboard/services', payload).then((r) => r.data)

export const updateService = (id: number, payload: ServiceInput) =>
  api.put(`/dashboard/services/${id}`, payload).then((r) => r.data)

export const deleteService = (id: number) => api.delete(`/dashboard/services/${id}`).then((r) => r.data)

export const toggleAvailability = (serviceId: number, date: string, isActive: boolean) =>
  api.post('/dashboard/availability/toggle', { serviceId, date, isActive }).then((r) => r.data)

export const getCalendar = (from: string, to: string, serviceId?: number) =>
  api.get<CalendarDay[]>('/dashboard/calendar', { params: { from, to, serviceId } }).then((r) => r.data)

export const setSlotOverride = (payload: {
  timeSlotId: number
  date: string
  isBlocked: boolean
  capacity: number | null
  note: string | null
}) => api.put('/dashboard/calendar/slot-override', payload).then((r) => r.data)

export const getDashboardOverview = () =>
  api.get<DashboardOverview>('/dashboard/bookings/overview').then((r) => r.data)

export const getRestaurantBookings = (date?: string, status?: string) =>
  api.get<RestaurantBookingGroup[]>('/dashboard/bookings', { params: { date, status } }).then((r) => r.data)

export const markBookingStatus = (id: number, status: string) =>
  api.patch(`/dashboard/bookings/${id}/status`, { status }).then((r) => r.data)

export const getBookingAnalytics = (start: string, end: string) =>
  api.get<BookingAnalytics>('/dashboard/bookings/analytics', { params: { start, end } }).then((r) => r.data)
