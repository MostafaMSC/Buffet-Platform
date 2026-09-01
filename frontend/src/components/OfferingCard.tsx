import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import type { OfferingListItem } from '../types'

export function OfferingCard({ offering }: { offering: OfferingListItem }) {
  const { t, i18n } = useTranslation()
  const isAr = i18n.language === 'ar'
  const restaurantName = isAr ? offering.restaurantNameAr : offering.restaurantName
  const areaName = isAr ? offering.areaNameAr : offering.areaNameEn

  return (
    <Link to={`/restaurants/${offering.restaurantId}`} className="offering-card">
      <div className="offering-card-photo">
        {offering.coverPhotoUrl && <img src={offering.coverPhotoUrl} alt={restaurantName} loading="lazy" />}
      </div>
      <div className="offering-card-body">
        <span className="badge">{t(`mealType.${offering.mealType}`)}</span>
        <div className="offering-card-title">{restaurantName}</div>
        <div className="offering-card-meta">
          <span>{areaName}</span>
          <span>
            {offering.opensAt}–{offering.closesAt}
          </span>
        </div>
        <div className="offering-card-price">
          {t('results.from')} {offering.price.toLocaleString()} {t('results.iqd')}
        </div>
      </div>
    </Link>
  )
}
