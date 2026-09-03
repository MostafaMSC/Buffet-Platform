import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { getServiceDetail } from '../api/endpoints'
import { ServiceCard } from '../components/ServiceCard'
import { CardSkeleton, EmptyState } from '../components/ui'
import { useFavorites } from '../hooks/useFavorites'
import type { ServiceCard as ServiceCardType } from '../types'

/// Saved services are ids on the device, so the page fetches each one to show a live card —
/// price and availability stay current rather than being frozen at the moment it was saved.
export function Favorites() {
  const { t } = useTranslation()
  const { ids } = useFavorites()
  const [cards, setCards] = useState<ServiceCardType[] | null>(null)

  useEffect(() => {
    if (ids.length === 0) {
      setCards([])
      return
    }

    let cancelled = false
    setCards(null)

    Promise.all(ids.map((id) => getServiceDetail(id).catch(() => null)))
      .then((details) => {
        if (cancelled) return
        setCards(
          details.filter(Boolean).map((d) => {
            const detail = d!
            return {
              serviceId: detail.id,
              serviceType: detail.serviceType,
              name: detail.name,
              nameAr: detail.nameAr,
              description: detail.description,
              descriptionAr: detail.descriptionAr,
              restaurantId: detail.restaurant.id,
              restaurantName: detail.restaurant.name,
              restaurantNameAr: detail.restaurant.nameAr,
              areaName: detail.restaurant.areaName,
              areaNameAr: detail.restaurant.areaNameAr,
              cityName: detail.restaurant.cityName,
              cityNameAr: detail.restaurant.cityNameAr,
              citySlug: detail.restaurant.citySlug,
              latitude: detail.restaurant.latitude,
              longitude: detail.restaurant.longitude,
              photoUrl: detail.photoUrls[0] ?? detail.restaurant.coverPhotoUrl,
              mealType: detail.mealType,
              cuisines: detail.cuisines,
              dietary: detail.dietary,
              pricingModel: detail.pricingModel,
              price: detail.pricingModel === 'PerPackage' ? detail.packagePrice ?? 0 : detail.pricePerAdult,
              priceChild: detail.pricePerChild,
              packageGuests: detail.packageGuests,
              currencyCode: detail.currencyCode,
              rating: detail.restaurant.rating,
              reviewCount: detail.restaurant.reviewCount,
              opensAt: detail.opensAt,
              closesAt: detail.closesAt,
              durationMinutes: detail.durationMinutes,
              minGuests: detail.minGuests,
              maxGuests: detail.maxGuests,
              isAvailable: detail.availability.slots.some((s) => !s.isFull && !s.isPast),
              spotsLeft: detail.availability.slots.length
                ? Math.max(...detail.availability.slots.map((s) => s.remaining))
                : null,
              nextAvailableTime: detail.availability.slots.find((s) => !s.isFull && !s.isPast)?.startTime ?? null,
              bookingEnabled: detail.availability.bookingEnabled,
              bookingMode: detail.bookingMode,
              isFoundingRestaurant: false,
              recentBookings: 0,
            } satisfies ServiceCardType
          }),
        )
      })

    return () => { cancelled = true }
  }, [ids])

  return (
    <div className="container section">
      <div className="section-head">
        <div>
          <h1>{t('favorites.title')}</h1>
          <p>{t('favorites.subtitle')}</p>
        </div>
      </div>

      {cards === null && (
        <div className="card-grid">{Array.from({ length: 4 }, (_, i) => <CardSkeleton key={i} />)}</div>
      )}

      {cards?.length === 0 && (
        <EmptyState
          icon="🤍"
          title={t('favorites.empty')}
          message={t('favorites.emptyText')}
          actions={<Link className="btn" to="/search">{t('favorites.browse')}</Link>}
        />
      )}

      {cards && cards.length > 0 && (
        <div className="card-grid">
          {cards.map((card) => <ServiceCard key={card.serviceId} card={card} />)}
        </div>
      )}
    </div>
  )
}
