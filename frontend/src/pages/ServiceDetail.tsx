import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams, useSearchParams } from 'react-router-dom'
import { getServiceDetail } from '../api/endpoints'
import { BookingPanel } from '../components/BookingPanel'
import { ServiceCard } from '../components/ServiceCard'
import { Badge, EmptyState, RatingInline, ServiceTypeBadge, Skeleton, Stars } from '../components/ui'
import type { ServiceDetail as ServiceDetailType } from '../types'
import { formatDate, formatTime, money, durationLabel } from '../utils/format'
import { getVideoEmbedUrl, isDirectVideoFile } from '../utils/video'

export function ServiceDetail() {
  const { id } = useParams()
  const serviceId = Number(id)
  const { t, i18n } = useTranslation()
  const isAr = i18n.language === 'ar'
  const [params] = useSearchParams()

  const [detail, setDetail] = useState<ServiceDetailType | null>(null)
  const [failed, setFailed] = useState(false)
  const [lightbox, setLightbox] = useState<number | null>(null)

  const dateParam = params.get('date') ?? undefined
  const guestsParam = Number(params.get('guests') ?? 2)

  useEffect(() => {
    setDetail(null)
    setFailed(false)
    getServiceDetail(serviceId, dateParam, Math.max(1, guestsParam), 0)
      .then(setDetail)
      .catch(() => setFailed(true))
  }, [serviceId, dateParam, guestsParam])

  if (failed) {
    return (
      <div className="container section">
        <EmptyState
          icon="🔍"
          title={t('search.errorTitle')}
          message={t('search.errorText')}
          actions={<Link className="btn secondary" to="/search">{t('nav.explore')}</Link>}
        />
      </div>
    )
  }

  if (!detail) {
    return (
      <div className="container section">
        <Skeleton height={320} radius={20} />
        <div className="stack stack-3" style={{ marginTop: 'var(--sp-5)', maxWidth: 620 }}>
          <Skeleton height={28} width="60%" />
          <Skeleton height={16} width="40%" />
          <Skeleton height={90} />
        </div>
      </div>
    )
  }

  const name = isAr ? detail.name : detail.name
  const displayName = isAr ? detail.nameAr : detail.name
  const description = isAr ? detail.descriptionAr : detail.description
  const restaurantName = isAr ? detail.restaurant.nameAr : detail.restaurant.name
  const area = isAr ? detail.restaurant.areaNameAr : detail.restaurant.areaName
  const city = isAr ? detail.restaurant.cityNameAr : detail.restaurant.cityName
  const photos = detail.photoUrls.length > 0 ? detail.photoUrls : [detail.restaurant.coverPhotoUrl].filter(Boolean) as string[]
  void name

  const servedOn = detail.recurrence === 'Daily'
    ? t('detail.everyDay')
    : detail.recurrence === 'SpecificWeekdays'
      ? detail.weekdays.map((d) => t(`weekday.${d}`)).join(' · ')
      : detail.recurrence === 'RamadanMode' && detail.ramadanStartDate && detail.ramadanEndDate
        ? t('detail.ramadanRange', {
            from: formatDate(detail.ramadanStartDate, i18n.language),
            to: formatDate(detail.ramadanEndDate, i18n.language),
          })
        : detail.oneOffDate
          ? formatDate(detail.oneOffDate, i18n.language)
          : t('detail.everyDay')

  const embedUrl = detail.videoUrl ? getVideoEmbedUrl(detail.videoUrl) : null

  return (
    <>
      {photos.length > 0 && (
        <div className="container" style={{ paddingTop: 'var(--sp-4)' }}>
          <div className="gallery" data-count={Math.min(photos.length, 5)}>
            {photos.slice(0, 5).map((url, i) => (
              <button key={url} onClick={() => setLightbox(i)} aria-label={t('detail.viewAllPhotos', { count: photos.length })}>
                <img src={url} alt="" loading={i === 0 ? 'eager' : 'lazy'} />
              </button>
            ))}
          </div>
        </div>
      )}

      <div className="container section-tight">
        <div className="detail-layout">
          <div className="stack stack-6">
            <div className="stack stack-3">
              <div className="row wrap" style={{ gap: 'var(--sp-2)' }}>
                <ServiceTypeBadge type={detail.serviceType} />
                <Badge>{t(`mealType.${detail.mealType}`)}</Badge>
                {detail.bookingMode === 'Instant' && <Badge kind="good">{t('bookingMode.Instant')}</Badge>}
                {detail.dietary.map((tag) => <Badge key={tag}>{t(`dietary.${tag}`)}</Badge>)}
              </div>

              <h1>{displayName}</h1>

              <div className="row wrap small soft" style={{ gap: 'var(--sp-3)' }}>
                <RatingInline rating={detail.restaurant.rating} reviewCount={detail.restaurant.reviewCount} />
                <span>·</span>
                <Link className="link" to={`/restaurants/${detail.restaurant.id}`}>{restaurantName}</Link>
                <span>·</span>
                <span>{area}, {city}</span>
              </div>

              {description && <p className="soft" style={{ maxWidth: '68ch' }}>{description}</p>}
            </div>

            <div className="facts">
              <div className="fact">
                <span className="label">{t('detail.servedOn')}</span>
                <span className="value">{servedOn}</span>
              </div>
              <div className="fact">
                <span className="label">{t('detail.servingHours')}</span>
                <span className="value">{formatTime(detail.opensAt, i18n.language)} – {formatTime(detail.closesAt, i18n.language)}</span>
              </div>
              {detail.durationMinutes && (
                <div className="fact">
                  <span className="label">{t('detail.duration')}</span>
                  <span className="value">{durationLabel(detail.durationMinutes, t)}</span>
                </div>
              )}
              <div className="fact">
                <span className="label">{t('detail.partySize')}</span>
                <span className="value">
                  {detail.maxGuests
                    ? t('detail.partySizeValue', { min: detail.minGuests, max: detail.maxGuests })
                    : t('detail.partySizeMin', { min: detail.minGuests })}
                </span>
              </div>
            </div>

            {detail.cuisines.length > 0 && (
              <div className="pill-row">
                {detail.cuisines.map((cuisine) => <span key={cuisine} className="chip sm">{t(`cuisine.${cuisine}`)}</span>)}
              </div>
            )}

            {detail.videoUrl && (
              <section>
                <h2 style={{ marginBottom: 'var(--sp-3)' }}>{t('detail.watchVideo')}</h2>
                <div style={{ borderRadius: 'var(--r-md)', overflow: 'hidden', border: '1px solid var(--c-line)' }}>
                  {isDirectVideoFile(detail.videoUrl) ? (
                    <video src={detail.videoUrl} controls playsInline style={{ width: '100%', display: 'block' }} />
                  ) : embedUrl ? (
                    <iframe
                      src={embedUrl}
                      title={displayName}
                      style={{ width: '100%', aspectRatio: '16 / 9', border: 0 }}
                      allow="autoplay; encrypted-media; picture-in-picture"
                      allowFullScreen
                    />
                  ) : null}
                </div>
              </section>
            )}

            {detail.menu.length > 0 && (
              <section>
                <h2 style={{ marginBottom: 'var(--sp-4)' }}>{t('detail.menu')}</h2>
                {detail.menu.map((section) => (
                  <div className="menu-section" key={section.id}>
                    <h3>{isAr ? section.nameAr : section.name}</h3>
                    <div style={{ marginTop: 'var(--sp-2)' }}>
                      {section.items.map((item) => (
                        <div className="menu-item" key={item.id}>
                          <div>
                            <div className="strong small">{isAr ? item.nameAr : item.name}</div>
                            {(isAr ? item.descriptionAr : item.description) && (
                              <div className="tiny muted">{isAr ? item.descriptionAr : item.description}</div>
                            )}
                          </div>
                          <div className="row" style={{ gap: 4 }}>
                            {item.dietary.map((tag) => <span key={tag} className="badge">{t(`dietary.${tag}`)}</span>)}
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                ))}
              </section>
            )}

            <section>
              <h2 style={{ marginBottom: 'var(--sp-3)' }}>{t('detail.theRestaurant')}</h2>
              <div className="card card-pad stack stack-4">
                <div className="row-between wrap">
                  <div className="stack stack-1">
                    <strong>{restaurantName}</strong>
                    <span className="small muted">{detail.restaurant.address ?? `${area}, ${city}`}</span>
                  </div>
                  <div className="row" style={{ gap: 'var(--sp-2)' }}>
                    <a className="btn secondary sm" href={`tel:${detail.restaurant.phoneNumber}`}>{t('detail.call')}</a>
                    {detail.restaurant.googleMapsUrl && (
                      <a className="btn secondary sm" href={detail.restaurant.googleMapsUrl} target="_blank" rel="noreferrer">
                        {t('detail.directions')}
                      </a>
                    )}
                  </div>
                </div>

                {(isAr ? detail.restaurant.descriptionAr : detail.restaurant.description) && (
                  <p className="small soft">{isAr ? detail.restaurant.descriptionAr : detail.restaurant.description}</p>
                )}

                {detail.restaurant.features.length > 0 && (
                  <div>
                    <h4 className="eyebrow" style={{ marginBottom: 'var(--sp-2)' }}>{t('detail.facilities')}</h4>
                    <div className="pill-row">
                      {detail.restaurant.features.map((f) => <span key={f} className="chip sm">{t(`feature.${f}`)}</span>)}
                    </div>
                  </div>
                )}

                <div>
                  <h4 className="eyebrow" style={{ marginBottom: 'var(--sp-2)' }}>{t('detail.policies')}</h4>
                  <ul className="stack stack-2 small soft" style={{ margin: 0, paddingInlineStart: '1.1rem' }}>
                    <li>{t('detail.cancellation', { minutes: detail.cancellationCutoffMinutes })}</li>
                    {detail.minAdvanceMinutes > 0 && <li>{t('detail.advanceNotice', { minutes: detail.minAdvanceMinutes })}</li>}
                    <li>{detail.bookingMode === 'Instant' ? t('bookingMode.instantHint') : t('bookingMode.requestHint')}</li>
                    {detail.freeUnderAge && <li>{t('price.freeUnder', { age: detail.freeUnderAge })}</li>}
                  </ul>
                </div>
              </div>
            </section>

            <section>
              <div className="row-between" style={{ marginBottom: 'var(--sp-3)' }}>
                <h2>{t('detail.reviews')}</h2>
                <RatingInline rating={detail.restaurant.rating} reviewCount={detail.restaurant.reviewCount} />
              </div>
              {detail.reviews.length === 0 ? (
                <p className="small muted">{t('detail.noReviews')}</p>
              ) : (
                <div className="card card-pad">
                  {detail.reviews.map((review) => (
                    <div className="review" key={review.id}>
                      <div className="row-between">
                        <strong className="small">{review.customerName}</strong>
                        <span style={{ color: 'var(--c-gold-dark)' }}><Stars rating={review.rating} size={13} /></span>
                      </div>
                      {review.comment && <p className="small soft" style={{ marginTop: 4 }}>{review.comment}</p>}
                      {review.isVerified && <span className="badge good" style={{ marginTop: 6 }}>{t('rating.verified')}</span>}
                    </div>
                  ))}
                </div>
              )}
            </section>
          </div>

          <BookingPanel detail={detail} initialDate={dateParam} initialGuests={guestsParam} />
        </div>
      </div>

      {detail.similarServices.length > 0 && (
        <section className="container section">
          <div className="section-head"><h2>{t('detail.similar')}</h2></div>
          <div className="card-grid">
            {detail.similarServices.map((card) => <ServiceCard key={card.serviceId} card={card} />)}
          </div>
        </section>
      )}

      {lightbox !== null && (
        <div className="sheet-backdrop" onClick={() => setLightbox(null)} role="presentation">
          <img
            src={photos[lightbox]}
            alt=""
            style={{ maxHeight: '86vh', maxWidth: '92vw', borderRadius: 'var(--r-md)', objectFit: 'contain' }}
            onClick={(e) => e.stopPropagation()}
          />
        </div>
      )}
    </>
  )
}

/// Shown under the price on the panel — keeps the money maths visible rather than
/// presenting a single unexplained total.
export function PriceLine({ label, amount, currency }: { label: string; amount: number; currency: string }) {
  const { i18n } = useTranslation()
  return (
    <div className="price-row">
      <span className="soft">{label}</span>
      <span className="nums">{money(amount, currency, i18n.language)}</span>
    </div>
  )
}
