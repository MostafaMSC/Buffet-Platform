import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { useFavorites } from '../hooks/useFavorites'
import type { ServiceCard as ServiceCardType } from '../types'
import { formatTime, priceLabel } from '../utils/format'
import { AvailabilityPill, HeartIcon, RatingInline, ServiceTypeBadge } from './ui'

/// The result card. Deliberately restrained: image, who and where, service type, price,
/// rating and whether the party can sit. Everything else is a tap away on the detail page.
export function ServiceCard({ card, searchQuery }: { card: ServiceCardType; searchQuery?: string }) {
  const { t, i18n } = useTranslation()
  const isAr = i18n.language === 'ar'
  const { isFavorite, toggle } = useFavorites()

  const name = isAr ? card.nameAr : card.name
  const restaurant = isAr ? card.restaurantNameAr : card.restaurantName
  const area = isAr ? card.areaNameAr : card.areaName
  const city = isAr ? card.cityNameAr : card.cityName
  const href = `/services/${card.serviceId}${searchQuery ? `?${searchQuery}` : ''}`

  return (
    <article className="service-card">
      <Link to={href} className="service-card-media" aria-label={name}>
        {card.photoUrl ? (
          <img src={card.photoUrl} alt="" loading="lazy" />
        ) : (
          <div style={{ width: '100%', height: '100%', display: 'grid', placeItems: 'center', fontSize: '2rem' }} aria-hidden>
            🍽️
          </div>
        )}
        <div className="overlay-top">
          <ServiceTypeBadge type={card.serviceType} />
          {card.isFoundingRestaurant && <span className="badge solid">{t('badge.founding')}</span>}
        </div>
      </Link>

      <button
        type="button"
        className={`fav-btn ${isFavorite(card.serviceId) ? 'on' : ''}`}
        style={{ position: 'absolute', insetBlockStart: 'var(--sp-3)', insetInlineEnd: 'var(--sp-3)' }}
        onClick={() => toggle(card.serviceId)}
        aria-pressed={isFavorite(card.serviceId)}
        aria-label={t(isFavorite(card.serviceId) ? 'favorites.remove' : 'favorites.add')}
      >
        <HeartIcon filled={isFavorite(card.serviceId)} />
      </button>

      <Link to={href} className="service-card-body">
        <div className="row-between" style={{ alignItems: 'baseline' }}>
          <span className="service-card-title truncate">{restaurant}</span>
          <RatingInline rating={card.rating} reviewCount={card.reviewCount} />
        </div>

        <div className="small soft truncate">{name}</div>
        <div className="tiny muted truncate">
          {area}, {city} · {formatTime(card.opensAt, i18n.language)}–{formatTime(card.closesAt, i18n.language)}
        </div>

        <div className="row-between" style={{ marginTop: 'var(--sp-2)' }}>
          <span className="service-card-price">
            <b>{priceLabel(card, t, i18n.language)}</b>
          </span>
          <AvailabilityPill
            isAvailable={card.isAvailable}
            spotsLeft={card.spotsLeft}
            bookingEnabled={card.bookingEnabled}
          />
        </div>
      </Link>
    </article>
  )
}
