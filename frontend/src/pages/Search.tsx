import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router-dom'
import { getLocations, searchServices, type SearchParams } from '../api/endpoints'
import { CategoryNav, type CategoryValue } from '../components/CategoryNav'
import { FilterPanel, activeFilterCount } from '../components/FilterPanel'
import { SearchBar, SearchSummary, type SearchBarValue } from '../components/SearchBar'
import { ServiceCard } from '../components/ServiceCard'
import { CardSkeleton, EmptyState, Icon, Select, Sheet } from '../components/ui'
import type { CountryOption, SearchResults, SearchSort } from '../types'
import { addDays, formatDate, todayInBaghdad } from '../utils/format'

const SORTS: SearchSort[] = ['Recommended', 'PriceLowToHigh', 'PriceHighToLow', 'Rating', 'Popular', 'Distance']

/// Search state lives entirely in the URL, so a search can be shared, bookmarked, reloaded
/// and stepped back through with the browser's own buttons.
export function Search() {
  const { t, i18n } = useTranslation()
  const [params, setParams] = useSearchParams()

  const [results, setResults] = useState<SearchResults | null>(null)
  const [loading, setLoading] = useState(true)
  const [failed, setFailed] = useState(false)
  const [showFilters, setShowFilters] = useState(false)
  const [showMap, setShowMap] = useState(false)
  const [showSearchSheet, setShowSearchSheet] = useState(false)
  const [highlighted, setHighlighted] = useState<number | null>(null)
  const requestId = useRef(0)
  const [countries, setCountries] = useState<CountryOption[]>([])

  useEffect(() => { getLocations().then(setCountries).catch(() => setCountries([])) }, [])

  const get = useCallback((key: string) => params.get(key) ?? undefined, [params])
  const getAll = useCallback((key: string) => params.getAll(key), [params])

  const query: SearchParams = useMemo(() => ({
    type: get('type'),
    city: get('city'),
    areaId: get('areaId') ? Number(get('areaId')) : undefined,
    date: get('date'),
    time: get('time'),
    timeOfDay: get('timeOfDay'),
    guests: get('guests') ? Number(get('guests')) : undefined,
    minPrice: get('minPrice') ? Number(get('minPrice')) : undefined,
    maxPrice: get('maxPrice') ? Number(get('maxPrice')) : undefined,
    cuisines: getAll('cuisines'),
    dietary: getAll('dietary'),
    features: getAll('features'),
    mealTypes: getAll('mealTypes'),
    bookingMode: get('bookingMode'),
    minRating: get('minRating') ? Number(get('minRating')) : undefined,
    availability: get('availability'),
    sort: get('sort') ?? 'Recommended',
    maxDistanceKm: get('maxDistanceKm') ? Number(get('maxDistanceKm')) : undefined,
    lat: get('lat') ? Number(get('lat')) : undefined,
    lng: get('lng') ? Number(get('lng')) : undefined,
    q: get('q'),
    pageSize: 24,
  }), [get, getAll])

  // One request per change, and a stale response can never overwrite a newer one.
  useEffect(() => {
    const id = ++requestId.current
    const controller = new AbortController()
    setLoading(true)
    setFailed(false)

    searchServices(query, controller.signal)
      .then((data) => { if (id === requestId.current) { setResults(data); setLoading(false) } })
      .catch((err) => {
        if (controller.signal.aborted || id !== requestId.current) return
        if ((err as { code?: string }).code === 'ERR_CANCELED') return
        setFailed(true)
        setLoading(false)
      })

    return () => controller.abort()
  }, [query])

  const patch = (changes: Record<string, string | string[] | undefined>) => {
    const next = new URLSearchParams(params)
    for (const [key, value] of Object.entries(changes)) {
      next.delete(key)
      if (Array.isArray(value)) value.forEach((v) => next.append(key, v))
      else if (value !== undefined && value !== '') next.set(key, value)
    }
    setParams(next, { replace: false })
  }

  const searchValue: SearchBarValue = {
    city: query.city ?? '',
    areaId: query.areaId,
    date: query.date ?? '',
    time: query.time ?? '',
    guests: query.guests ?? 2,
  }

  const category: CategoryValue = (query.type as CategoryValue) ?? 'All'
  const filterCount = activeFilterCount(params)
  const cityLabel = query.city ? t(`city.${query.city}`, { defaultValue: query.city }) : null
  const areaLabel = query.areaId
    ? countries.flatMap((c) => c.cities).flatMap((c) => c.areas).find((a) => a.id === query.areaId)
    : undefined
  const locationLabel = areaLabel
    ? `${i18n.language === 'ar' ? areaLabel.nameAr : areaLabel.nameEn}, ${cityLabel}`
    : (cityLabel ?? undefined)

  return (
    <>
      <div className="results-head">
        <div className="container">
          <div className="stack stack-3" style={{ paddingTop: 'var(--sp-4)' }}>
            <div className="desktop-search">
              <SearchBar
                value={searchValue}
                onChange={(next) => patch({
                  city: next.city || undefined,
                  areaId: next.areaId ? String(next.areaId) : undefined,
                  date: next.date || undefined,
                  time: next.time || undefined,
                  guests: String(next.guests),
                })}
                onSubmit={() => { /* URL already reflects each change */ }}
              />
            </div>

            {/* On a phone the full search bar has too many controls to sit permanently in a
                sticky header, so it collapses to a single summary row that opens the same
                bar in a sheet. */}
            <div className="mobile-search-trigger">
              <SearchSummary value={searchValue} locationLabel={locationLabel} onClick={() => setShowSearchSheet(true)} />
            </div>

            <CategoryNav value={category} onChange={(next) => patch({ type: next === 'All' ? undefined : next })} />
          </div>

          <div className="filter-bar">
            <button className="chip" onClick={() => setShowFilters(true)}>
              <Icon name="filter" size={15} />
              {t('filters.open')}
              {filterCount > 0 && <span className="badge buffet">{filterCount}</span>}
            </button>

            <Select
              className="chip"
              value={query.sort}
              onChange={(e) => patch({ sort: e.target.value })}
              aria-label={t('filters.sort')}
            >
              {SORTS.map((sort) => <option key={sort} value={sort}>{t(`sort.${sort}`)}</option>)}
            </Select>

            <button
              className={`chip ${query.availability === 'Today' ? 'active' : ''}`}
              onClick={() => patch({ availability: query.availability === 'Today' ? undefined : 'Today' })}
            >
              {t('availabilityWindow.Today')}
            </button>

            <button
              className={`chip ${query.bookingMode === 'Instant' ? 'active' : ''}`}
              onClick={() => patch({ bookingMode: query.bookingMode === 'Instant' ? undefined : 'Instant' })}
            >
              {t('bookingMode.Instant')}
            </button>

            <button className="chip desktop-only" onClick={() => setShowMap((v) => !v)}>
              <Icon name="map" size={15} />
              {showMap ? t('search.hideMap') : t('search.showMap')}
            </button>
          </div>
        </div>
      </div>

      <div className={showMap ? 'map-split' : ''}>
        <div className="container section-tight">
          <div className="section-head">
            <div>
              <h1 style={{ fontSize: '1.4rem' }}>
                {query.type === 'Buffet'
                  ? t('search.resultsTitleBuffet')
                  : query.type === 'SetMenu'
                    ? t('search.resultsTitleSetMenu')
                    : t('search.resultsTitle')}{' '}
                {cityLabel ? t('search.resultsIn', { city: cityLabel }) : ''}
              </h1>
              <p>
                {query.date ? `${formatDate(query.date, i18n.language)} · ` : ''}
                {t('search.guestCount', { count: query.guests ?? 2 })}
                {results ? ` · ${t('search.resultsCount', { count: results.total })}` : ''}
              </p>
            </div>
          </div>

          {loading && (
            <div className="card-grid">
              {Array.from({ length: 8 }, (_, i) => <CardSkeleton key={i} />)}
            </div>
          )}

          {!loading && failed && (
            <EmptyState
              icon="⚠️"
              title={t('search.errorTitle')}
              message={t('search.errorText')}
              actions={<button className="btn" onClick={() => patch({ _r: String(Date.now()) })}>{t('common.retry')}</button>}
            />
          )}

          {!loading && !failed && results && results.items.length === 0 && (
            <EmptyState
              title={t('search.noResultsTitle')}
              message={t('search.noResultsText')}
              actions={
                <>
                  <button className="btn secondary" onClick={() => patch({ date: addDays(query.date ?? todayInBaghdad(), 1) })}>
                    {t('search.changeDate')}
                  </button>
                  <button className="btn secondary" onClick={() => setParams(new URLSearchParams(query.type ? { type: query.type } : {}))}>
                    {t('search.clearFilters')}
                  </button>
                  <button className="btn secondary" onClick={() => patch({ city: undefined, areaId: undefined })}>
                    {t('search.searchNearby')}
                  </button>
                </>
              }
            />
          )}

          {!loading && !failed && results && results.items.length > 0 && (
            <div className="card-grid">
              {results.items.map((card) => (
                <div
                  key={card.serviceId}
                  onMouseEnter={() => setHighlighted(card.serviceId)}
                  onMouseLeave={() => setHighlighted(null)}
                >
                  <ServiceCard card={card} searchQuery={params.toString()} />
                </div>
              ))}
            </div>
          )}
        </div>

        {showMap && results && (
          <aside className="map-pane" aria-label={t('map.title')}>
            <div className="map-placeholder">
              <Icon name="map" size={28} />
              <p style={{ marginTop: 'var(--sp-2)' }}>{t('map.comingSoon')}</p>
              <div className="map-pin-list">
                {results.items.filter((c) => c.latitude && c.longitude).map((card) => (
                  <div key={card.serviceId} className={`map-pin ${highlighted === card.serviceId ? 'active' : ''}`}>
                    <span className="truncate">{i18n.language === 'ar' ? card.restaurantNameAr : card.restaurantName}</span>
                    <span className="nums muted">{card.latitude?.toFixed(3)}, {card.longitude?.toFixed(3)}</span>
                  </div>
                ))}
              </div>
            </div>
          </aside>
        )}
      </div>

      {showFilters && (
        <FilterPanel
          params={params}
          resultCount={results?.total ?? 0}
          onApply={(next) => { setParams(next); setShowFilters(false) }}
          onClose={() => setShowFilters(false)}
        />
      )}

      {showSearchSheet && (
        <Sheet title={t('search.title')} onClose={() => setShowSearchSheet(false)}>
          <SearchBar
            value={searchValue}
            onChange={(next) => patch({
              city: next.city || undefined,
              areaId: next.areaId ? String(next.areaId) : undefined,
              date: next.date || undefined,
              time: next.time || undefined,
              guests: String(next.guests),
            })}
            onSubmit={() => setShowSearchSheet(false)}
          />
        </Sheet>
      )}
    </>
  )
}
