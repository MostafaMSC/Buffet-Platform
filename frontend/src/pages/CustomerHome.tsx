import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { api } from '../api/client'
import { FilterBar } from '../components/FilterBar'
import { OfferingCard } from '../components/OfferingCard'
import type { Area, MealType, OfferingListItem } from '../types'
import { dateWithOffset } from '../utils/date'

export function CustomerHome() {
  const { t } = useTranslation()
  const [areas, setAreas] = useState<Area[]>([])
  const [areaId, setAreaId] = useState<number | ''>('')
  const [mealType, setMealType] = useState<MealType | ''>('')
  const [dateOffset, setDateOffset] = useState<0 | 1>(0)
  const [offerings, setOfferings] = useState<OfferingListItem[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(false)

  useEffect(() => {
    api.get<Area[]>('/areas').then((res) => setAreas(res.data))
  }, [])

  useEffect(() => {
    const params: Record<string, string | number> = { date: dateWithOffset(dateOffset) }
    if (areaId) params.areaId = areaId
    if (mealType) params.mealType = mealType

    setLoading(true)
    setError(false)
    api
      .get<OfferingListItem[]>('/offerings', { params })
      .then((res) => setOfferings(res.data))
      .catch(() => setError(true))
      .finally(() => setLoading(false))
  }, [areaId, mealType, dateOffset])

  return (
    <div className="container">
      <div className="hero">
        <h1>{t('appName')}</h1>
        <p>{t('tagline')}</p>
      </div>

      <FilterBar
        areas={areas}
        areaId={areaId}
        onAreaChange={setAreaId}
        mealType={mealType}
        onMealTypeChange={setMealType}
        dateOffset={dateOffset}
        onDateOffsetChange={setDateOffset}
      />

      {loading && <p className="state-message">{t('results.loading')}</p>}
      {!loading && error && <p className="state-message">{t('common.error')}</p>}
      {!loading && !error && offerings.length === 0 && (
        <p className="state-message">{t('results.noResults')}</p>
      )}

      {!loading && !error && offerings.length > 0 && (
        <div className="results-grid">
          {offerings.map((o) => (
            <OfferingCard key={o.offeringId} offering={o} />
          ))}
        </div>
      )}
    </div>
  )
}
