import { useTranslation } from 'react-i18next'
import type { Area, MealType } from '../types'

interface Props {
  areas: Area[]
  areaId: number | ''
  onAreaChange: (areaId: number | '') => void
  mealType: MealType | ''
  onMealTypeChange: (mealType: MealType | '') => void
  dateOffset: 0 | 1
  onDateOffsetChange: (offset: 0 | 1) => void
}

const MEAL_TYPES: MealType[] = ['Breakfast', 'Lunch', 'Iftar', 'Sohor']

export function FilterBar({
  areas,
  areaId,
  onAreaChange,
  mealType,
  onMealTypeChange,
  dateOffset,
  onDateOffsetChange,
}: Props) {
  const { t, i18n } = useTranslation()
  const isAr = i18n.language === 'ar'

  return (
    <div className="filter-bar">
      <div className="date-quick-toggle">
        <button
          type="button"
          className={`chip ${dateOffset === 0 ? 'active' : ''}`}
          onClick={() => onDateOffsetChange(0)}
        >
          {t('filters.today')}
        </button>
        <button
          type="button"
          className={`chip ${dateOffset === 1 ? 'active' : ''}`}
          onClick={() => onDateOffsetChange(1)}
        >
          {t('filters.tomorrow')}
        </button>
      </div>

      <div className="filter-field">
        <label>{t('filters.area')}</label>
        <select
          value={areaId}
          onChange={(e) => onAreaChange(e.target.value ? Number(e.target.value) : '')}
        >
          <option value="">{t('filters.allAreas')}</option>
          {areas.map((a) => (
            <option key={a.id} value={a.id}>
              {isAr ? a.nameAr : a.nameEn}
            </option>
          ))}
        </select>
      </div>

      <div className="filter-field">
        <label>{t('filters.mealType')}</label>
        <select value={mealType} onChange={(e) => onMealTypeChange(e.target.value as MealType | '')}>
          <option value="">{t('filters.allMealTypes')}</option>
          {MEAL_TYPES.map((m) => (
            <option key={m} value={m}>
              {t(`mealType.${m}`)}
            </option>
          ))}
        </select>
      </div>
    </div>
  )
}
