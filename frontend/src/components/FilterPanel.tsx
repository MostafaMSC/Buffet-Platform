import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import {
  CUISINES,
  DIETARY_TAGS,
  MEAL_TYPES,
  RESTAURANT_FEATURES,
  type TimeOfDay,
} from '../types'
import { Sheet } from './ui'

const TIME_BANDS: TimeOfDay[] = ['Morning', 'Lunch', 'Afternoon', 'Dinner', 'LateNight']
const RATINGS = [4, 4.5]
const DISTANCES = [5, 10, 25]

/// Filters that actually narrow results, so the count in the apply button is honest.
const FILTER_KEYS = [
  'minPrice', 'maxPrice', 'timeOfDay', 'mealTypes', 'cuisines', 'dietary',
  'features', 'bookingMode', 'minRating', 'availability', 'maxDistanceKm',
]

export function activeFilterCount(params: URLSearchParams): number {
  return FILTER_KEYS.reduce((count, key) => count + (params.getAll(key).length > 0 ? 1 : 0), 0)
}

/// A full-screen sheet on mobile, a dialog on desktop. Changes are staged locally and
/// applied together — a filter panel that re-queries on every tap is unusable on a phone.
export function FilterPanel({
  params,
  resultCount,
  onApply,
  onClose,
}: {
  params: URLSearchParams
  resultCount: number
  onApply: (next: URLSearchParams) => void
  onClose: () => void
}) {
  const { t } = useTranslation()
  const [draft, setDraft] = useState(() => new URLSearchParams(params))

  const has = (key: string, value: string) => draft.getAll(key).includes(value)

  const toggleMulti = (key: string, value: string) => {
    const next = new URLSearchParams(draft)
    const current = next.getAll(key)
    next.delete(key)
    const updated = current.includes(value) ? current.filter((v) => v !== value) : [...current, value]
    updated.forEach((v) => next.append(key, v))
    setDraft(next)
  }

  const setSingle = (key: string, value: string | undefined) => {
    const next = new URLSearchParams(draft)
    if (value === undefined || value === '' || next.get(key) === value) next.delete(key)
    else next.set(key, value)
    setDraft(next)
  }

  const clearAll = () => {
    const next = new URLSearchParams(draft)
    FILTER_KEYS.forEach((key) => next.delete(key))
    setDraft(next)
  }

  const askForLocation = () => {
    navigator.geolocation?.getCurrentPosition((pos) => {
      const next = new URLSearchParams(draft)
      next.set('lat', pos.coords.latitude.toFixed(5))
      next.set('lng', pos.coords.longitude.toFixed(5))
      setDraft(next)
    })
  }

  return (
    <Sheet
      title={t('filters.title')}
      onClose={onClose}
      footer={
        <>
          <button className="btn ghost" onClick={clearAll}>{t('filters.clearAll')}</button>
          <button className="btn" onClick={() => onApply(draft)}>
            {t('filters.apply', { count: resultCount })}
          </button>
        </>
      }
    >
      <div className="filter-group">
        <h4>{t('filters.price')}</h4>
        <div className="form-grid">
          <label className="field">
            <span>{t('filters.priceMin')}</span>
            <input
              type="number"
              min={0}
              step={1000}
              placeholder="0"
              value={draft.get('minPrice') ?? ''}
              onChange={(e) => setSingle('minPrice', e.target.value || undefined)}
            />
          </label>
          <label className="field">
            <span>{t('filters.priceMax')}</span>
            <input
              type="number"
              min={0}
              step={1000}
              placeholder="100000"
              value={draft.get('maxPrice') ?? ''}
              onChange={(e) => setSingle('maxPrice', e.target.value || undefined)}
            />
          </label>
        </div>
      </div>

      <div className="filter-group">
        <h4>{t('filters.availability')}</h4>
        <div className="chip-wrap">
          {(['Today', 'ThisWeek', 'SelectedDate'] as const).map((option) => (
            <button
              key={option}
              className={`chip sm ${draft.get('availability') === option ? 'active' : ''}`}
              onClick={() => setSingle('availability', option)}
            >
              {t(`availabilityWindow.${option}`)}
            </button>
          ))}
        </div>
      </div>

      <div className="filter-group">
        <h4>{t('filters.time')}</h4>
        <div className="chip-wrap">
          {TIME_BANDS.map((band) => (
            <button
              key={band}
              className={`chip sm ${draft.get('timeOfDay') === band ? 'active' : ''}`}
              onClick={() => setSingle('timeOfDay', band)}
            >
              {t(`timeOfDay.${band}`)}
            </button>
          ))}
        </div>
      </div>

      <div className="filter-group">
        <h4>{t('filters.mealType')}</h4>
        <div className="chip-wrap">
          {MEAL_TYPES.map((meal) => (
            <button
              key={meal}
              className={`chip sm ${has('mealTypes', meal) ? 'active' : ''}`}
              onClick={() => toggleMulti('mealTypes', meal)}
            >
              {t(`mealType.${meal}`)}
            </button>
          ))}
        </div>
      </div>

      <div className="filter-group">
        <h4>{t('filters.cuisine')}</h4>
        <div className="chip-wrap">
          {CUISINES.map((cuisine) => (
            <button
              key={cuisine}
              className={`chip sm ${has('cuisines', cuisine) ? 'active' : ''}`}
              onClick={() => toggleMulti('cuisines', cuisine)}
            >
              {t(`cuisine.${cuisine}`)}
            </button>
          ))}
        </div>
      </div>

      <div className="filter-group">
        <h4>{t('filters.dietary')}</h4>
        <div className="chip-wrap">
          {DIETARY_TAGS.map((tag) => (
            <button
              key={tag}
              className={`chip sm ${has('dietary', tag) ? 'active' : ''}`}
              onClick={() => toggleMulti('dietary', tag)}
            >
              {t(`dietary.${tag}`)}
            </button>
          ))}
        </div>
      </div>

      <div className="filter-group">
        <h4>{t('filters.features')}</h4>
        <div className="chip-wrap">
          {RESTAURANT_FEATURES.map((feature) => (
            <button
              key={feature}
              className={`chip sm ${has('features', feature) ? 'active' : ''}`}
              onClick={() => toggleMulti('features', feature)}
            >
              {t(`feature.${feature}`)}
            </button>
          ))}
        </div>
      </div>

      <div className="filter-group">
        <h4>{t('filters.booking')}</h4>
        <div className="chip-wrap">
          {(['Instant', 'Request'] as const).map((mode) => (
            <button
              key={mode}
              className={`chip sm ${draft.get('bookingMode') === mode ? 'active' : ''}`}
              onClick={() => setSingle('bookingMode', mode)}
            >
              {t(`bookingMode.${mode}`)}
            </button>
          ))}
        </div>
      </div>

      <div className="filter-group">
        <h4>{t('filters.rating')}</h4>
        <div className="chip-wrap">
          {RATINGS.map((rating) => (
            <button
              key={rating}
              className={`chip sm ${draft.get('minRating') === String(rating) ? 'active' : ''}`}
              onClick={() => setSingle('minRating', String(rating))}
            >
              ★ {t('filters.ratingPlus', { rating })}
            </button>
          ))}
        </div>
      </div>

      <div className="filter-group">
        <h4>{t('filters.distance')}</h4>
        <div className="chip-wrap">
          {DISTANCES.map((km) => (
            <button
              key={km}
              className={`chip sm ${draft.get('maxDistanceKm') === String(km) ? 'active' : ''}`}
              onClick={() => { setSingle('maxDistanceKm', String(km)); if (!draft.get('lat')) askForLocation() }}
            >
              {t('distance.within', { km })}
            </button>
          ))}
        </div>
        {draft.get('maxDistanceKm') && !draft.get('lat') && (
          <p className="hint" style={{ marginTop: 'var(--sp-2)' }}>{t('distance.needLocation')}</p>
        )}
      </div>
    </Sheet>
  )
}
