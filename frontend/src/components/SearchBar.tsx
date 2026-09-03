import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { getLocations } from '../api/endpoints'
import type { CountryOption } from '../types'
import { formatDate, todayInBaghdad } from '../utils/format'
import { Icon } from './ui'

export interface SearchBarValue {
  city: string
  date: string
  time: string
  guests: number
}

const TIMES = ['', '08:00', '10:00', '12:00', '13:00', '14:00', '18:00', '19:00', '20:00', '21:00']

/// The one control that starts every journey: where, when, what time, how many. Kept to
/// four questions — anything more belongs in filters after results are on screen.
export function SearchBar({
  value,
  onChange,
  onSubmit,
  compact = false,
}: {
  value: SearchBarValue
  onChange: (next: SearchBarValue) => void
  onSubmit: () => void
  compact?: boolean
}) {
  const { t, i18n } = useTranslation()
  const [countries, setCountries] = useState<CountryOption[]>([])

  useEffect(() => {
    getLocations().then(setCountries).catch(() => setCountries([]))
  }, [])

  const cities = countries.flatMap((c) => c.cities)
  const today = todayInBaghdad()

  return (
    <form
      className="searchbar"
      onSubmit={(e) => { e.preventDefault(); onSubmit() }}
      role="search"
      aria-label={t('search.title')}
    >
      <label className="searchbar-field">
        <span className="label">{t('search.where')}</span>
        <select
          value={value.city}
          onChange={(e) => onChange({ ...value, city: e.target.value })}
          aria-label={t('search.where')}
        >
          <option value="">{t('search.anywhere')}</option>
          {cities.map((city) => (
            <option key={city.slug} value={city.slug}>
              {i18n.language === 'ar' ? city.nameAr : city.nameEn}
            </option>
          ))}
        </select>
      </label>

      <label className="searchbar-field">
        <span className="label">{t('search.when')}</span>
        <input
          type="date"
          min={today}
          value={value.date}
          onChange={(e) => onChange({ ...value, date: e.target.value })}
          aria-label={t('search.when')}
        />
      </label>

      {!compact && (
        <label className="searchbar-field">
          <span className="label">{t('search.time')}</span>
          <select
            value={value.time}
            onChange={(e) => onChange({ ...value, time: e.target.value })}
            aria-label={t('search.time')}
          >
            {TIMES.map((time) => (
              <option key={time || 'any'} value={time}>
                {time || t('search.anyTime')}
              </option>
            ))}
          </select>
        </label>
      )}

      <label className="searchbar-field">
        <span className="label">{t('search.guests')}</span>
        <select
          value={value.guests}
          onChange={(e) => onChange({ ...value, guests: Number(e.target.value) })}
          aria-label={t('search.guests')}
        >
          {Array.from({ length: 20 }, (_, i) => i + 1).map((n) => (
            <option key={n} value={n}>{t('search.guestCount', { count: n })}</option>
          ))}
        </select>
      </label>

      <button className="btn square" type="submit" aria-label={t('search.action')}>
        <Icon name="search" size={19} />
      </button>
    </form>
  )
}

/// A read-only summary of the current search, used as the tap target that reopens the
/// full search bar on small screens.
export function SearchSummary({ value, onClick }: { value: SearchBarValue; onClick: () => void }) {
  const { t, i18n } = useTranslation()
  const parts = [
    value.city ? value.city : t('search.anywhere'),
    value.date ? formatDate(value.date, i18n.language) : t('search.anyDate'),
    t('search.guestCount', { count: value.guests }),
  ]

  return (
    <button type="button" className="btn secondary block" onClick={onClick} style={{ justifyContent: 'flex-start', gap: 'var(--sp-3)' }}>
      <Icon name="search" size={17} />
      <span className="truncate">{parts.join(' · ')}</span>
    </button>
  )
}
