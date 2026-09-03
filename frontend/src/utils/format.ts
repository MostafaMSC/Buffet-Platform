import type { PricingModel, ServiceCard } from '../types'

/// Iraqi dinar is quoted in whole units — no decimals anywhere in the product.
export function money(amount: number, currency = 'IQD', locale = 'en'): string {
  const formatted = new Intl.NumberFormat(locale === 'ar' ? 'ar-IQ' : 'en-US', {
    maximumFractionDigits: 0,
  }).format(Math.round(amount))
  return `${formatted} ${currency}`
}

export function priceLabel(
  card: Pick<ServiceCard, 'price' | 'pricingModel' | 'packageGuests' | 'currencyCode'>,
  t: (key: string, opts?: Record<string, unknown>) => string,
  locale = 'en',
): string {
  const amount = money(card.price, card.currencyCode, locale)
  return card.pricingModel === 'PerPackage'
    ? t('price.perPackage', { amount, guests: card.packageGuests ?? 2 })
    : t('price.perPerson', { amount })
}

export function pricingUnit(model: PricingModel, guests: number | null, t: (k: string, o?: Record<string, unknown>) => string) {
  return model === 'PerPackage' ? t('price.forGuests', { guests: guests ?? 2 }) : t('price.person')
}

/// Baghdad runs UTC+3 year round, so "today" is computed against that rather than the
/// browser's zone — a customer abroad still sees the restaurant's day.
export function todayInBaghdad(): string {
  const now = new Date()
  const baghdad = new Date(now.getTime() + (3 * 60 - now.getTimezoneOffset()) * 60000)
  return baghdad.toISOString().slice(0, 10)
}

export function addDays(isoDate: string, days: number): string {
  const d = new Date(`${isoDate}T00:00:00`)
  d.setDate(d.getDate() + days)
  return d.toISOString().slice(0, 10)
}

export function formatDate(isoDate: string, locale = 'en', opts?: Intl.DateTimeFormatOptions): string {
  const d = new Date(`${isoDate}T00:00:00`)
  return d.toLocaleDateString(locale === 'ar' ? 'ar-IQ' : 'en-GB', opts ?? { weekday: 'short', day: 'numeric', month: 'short' })
}

export function formatDateLong(isoDate: string, locale = 'en'): string {
  return formatDate(isoDate, locale, { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' })
}

/// "19:30" → "7:30 PM" in English, kept as 24h in Arabic where that's the norm.
export function formatTime(time: string, locale = 'en'): string {
  if (!time) return ''
  if (locale === 'ar') return time
  const [h, m] = time.split(':').map(Number)
  const period = h >= 12 ? 'PM' : 'AM'
  const hour = h % 12 === 0 ? 12 : h % 12
  return `${hour}:${String(m).padStart(2, '0')} ${period}`
}

export function timeRange(from: string, to: string, locale = 'en'): string {
  return `${formatTime(from, locale)} – ${formatTime(to, locale)}`
}

export function durationLabel(minutes: number | null, t: (k: string, o?: Record<string, unknown>) => string): string | null {
  if (!minutes) return null
  const hours = Math.floor(minutes / 60)
  const mins = minutes % 60
  if (hours && mins) return t('duration.hoursMinutes', { hours, minutes: mins })
  if (hours) return t('duration.hours', { count: hours })
  return t('duration.minutes', { count: mins })
}

/// Reads an API error the way the backend reports it, falling back to a generic message.
export function apiError(err: unknown, fallback: string): string {
  const message = (err as { response?: { data?: { message?: string; errors?: Record<string, string[]> } } })?.response?.data
  if (message?.message) return message.message
  const firstValidation = message?.errors && Object.values(message.errors)[0]?.[0]
  return firstValidation ?? fallback
}
