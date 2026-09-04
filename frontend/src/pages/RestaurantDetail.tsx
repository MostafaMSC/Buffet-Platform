import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router-dom'
import { getRestaurant } from '../api/endpoints'
import { ServiceCard } from '../components/ServiceCard'
import { EmptyState, RatingInline, Skeleton, Stars } from '../components/ui'
import type { RestaurantPage } from '../types'

/// A restaurant's own page: the venue, then everything it currently offers as normal
/// bookable cards, so the route into a booking is the same as from search.
export function RestaurantDetail() {
  const { id } = useParams()
  const { t, i18n } = useTranslation()
  const isAr = i18n.language === 'ar'

  const [page, setPage] = useState<RestaurantPage | null>(null)
  const [failed, setFailed] = useState(false)

  useEffect(() => {
    getRestaurant(Number(id)).then(setPage).catch(() => setFailed(true))
  }, [id])

  if (failed) return <div className="container section"><EmptyState icon="🔎" title={t('common.error')} /></div>
  if (!page) return <div className="container section"><Skeleton height={260} radius={20} /></div>

  const r = page.restaurant
  const name = isAr ? r.nameAr : r.name
  const description = isAr ? r.descriptionAr : r.description

  return (
    <>
      {r.coverPhotoUrl && (
        <div className="container" style={{ paddingTop: 'var(--sp-4)' }}>
          <img
            src={r.coverPhotoUrl}
            alt=""
            style={{ width: '100%', aspectRatio: '21 / 8', objectFit: 'cover', borderRadius: 'var(--r-lg)' }}
          />
        </div>
      )}

      <div className="container section-tight stack stack-6">
        <div className="stack stack-3">
          <h1>{name}</h1>
          <div className="row wrap small soft" style={{ gap: 'var(--sp-3)' }}>
            <RatingInline rating={r.rating} reviewCount={r.reviewCount} />
            <span>·</span>
            <span>{isAr ? r.areaNameAr : r.areaName}, {isAr ? r.cityNameAr : r.cityName}</span>
          </div>
          {description && <p className="soft" style={{ maxWidth: '68ch' }}>{description}</p>}

          <div className="row wrap" style={{ gap: 'var(--sp-2)' }}>
            <a className="btn secondary sm" href={`tel:${r.phoneNumber}`}>{t('detail.call')}</a>
            {r.googleMapsUrl && (
              <a className="btn secondary sm" href={r.googleMapsUrl} target="_blank" rel="noreferrer">{t('detail.directions')}</a>
            )}
          </div>

          {r.features.length > 0 && (
            <div className="pill-row">
              {r.features.map((f) => <span key={f} className="chip sm">{t(`feature.${f}`)}</span>)}
            </div>
          )}
        </div>

        <section>
          <div className="section-head"><h2>{t('search.resultsTitle')}</h2></div>
          {page.services.length === 0 ? (
            <EmptyState icon="🍽️" title={t('search.noResultsTitle')} />
          ) : (
            <div className="card-grid">
              {page.services.map((card) => <ServiceCard key={card.serviceId} card={card} />)}
            </div>
          )}
        </section>

        {page.reviews.length > 0 && (
          <section>
            <div className="section-head"><h2>{t('rating.guestReviews')}</h2></div>
            <div className="card card-pad">
              {page.reviews.map((review) => (
                <div className="review" key={review.id}>
                  <div className="row-between">
                    <strong className="small">{review.customerName}</strong>
                    <span style={{ color: 'var(--c-gold-dark)' }}><Stars rating={review.rating} size={13} /></span>
                  </div>
                  {review.comment && <p className="small soft" style={{ marginTop: 4 }}>{review.comment}</p>}
                </div>
              ))}
            </div>
          </section>
        )}
      </div>
    </>
  )
}
