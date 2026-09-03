import { useTranslation } from 'react-i18next'
import type { ServiceType } from '../types'

export type CategoryValue = 'All' | ServiceType

const CATEGORIES: { value: CategoryValue; icon: string }[] = [
  { value: 'All', icon: '✨' },
  { value: 'Buffet', icon: '🍲' },
  { value: 'SetMenu', icon: '🍽️' },
]

/// The marketplace's primary navigation, not a tab strip: picking a category re-runs the
/// search, rewrites the URL and changes what the filters offer.
export function CategoryNav({
  value,
  onChange,
}: {
  value: CategoryValue
  onChange: (next: CategoryValue) => void
}) {
  const { t } = useTranslation()

  return (
    <div className="category-nav" role="tablist" aria-label={t('category.label')}>
      {CATEGORIES.map((category) => (
        <button
          key={category.value}
          role="tab"
          type="button"
          aria-selected={value === category.value}
          className={`category-tab ${value === category.value ? 'active' : ''}`}
          onClick={() => onChange(category.value)}
        >
          <span className="icon" aria-hidden>{category.icon}</span>
          <span>{category.value === 'All' ? t('category.all') : t(`serviceType.${category.value}`)}</span>
        </button>
      ))}
    </div>
  )
}
