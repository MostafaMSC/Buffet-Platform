import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router-dom'
import { api } from '../api/client'
import type { RestaurantDetail as RestaurantDetailType } from '../types'

export function RestaurantDetail() {
  const { id } = useParams()
  const { t, i18n } = useTranslation()
  const isAr = i18n.language === 'ar'
  const [restaurant, setRestaurant] = useState<RestaurantDetailType | null>(null)
  const [loading, setLoading] = useState(true)
  const [notFound, setNotFound] = useState(false)

  useEffect(() => {
    setLoading(true)
    setNotFound(false)
    api
      .get<RestaurantDetailType>(`/restaurants/${id}`)
      .then((res) => setRestaurant(res.data))
      .catch(() => setNotFound(true))
      .finally(() => setLoading(false))
  }, [id])

  if (loading) return <p className="state-message">{t('common.loading')}</p>
  if (notFound || !restaurant) return <p className="state-message">{t('results.noResults')}</p>

  const name = isAr ? restaurant.nameAr : restaurant.name
  const areaName = isAr ? restaurant.areaNameAr : restaurant.areaNameEn
  const description = isAr ? restaurant.descriptionAr : restaurant.description

  return (
    <div className="container">
      <Link to="/" className="nav-link" style={{ display: 'inline-block', marginBottom: '0.75rem' }}>
        ← {t('detail.back')}
      </Link>

      <div className="detail-cover">
        {restaurant.coverPhotoUrl && <img src={restaurant.coverPhotoUrl} alt={name} />}
      </div>

      <div className="detail-header">
        <h1>{name}</h1>
        <div className="area">{areaName}</div>
      </div>

      {description && <p>{description}</p>}

      <div className="action-row">
        <a className="action-btn primary" href={`tel:${restaurant.phoneNumber}`}>
          📞 {t('detail.call')}
        </a>
        {restaurant.googleMapsUrl && (
          <a className="action-btn" href={restaurant.googleMapsUrl} target="_blank" rel="noreferrer">
            📍 {t('detail.directions')}
          </a>
        )}
      </div>

      {restaurant.offerings.map((o) => {
        const desc = isAr ? o.descriptionAr : o.description
        return (
          <div className="offering-block" key={o.id}>
            <div className="offering-block-header">
              <span className="badge">{t(`mealType.${o.mealType}`)}</span>
              <span className={`status-pill ${o.isActiveToday ? 'on' : 'off'}`}>
                {o.isActiveToday ? t('detail.activeToday') : t('detail.notActiveToday')}
              </span>
            </div>
            <div className="offering-card-meta">
              <span>
                {t('detail.hours')}: {o.opensAt}–{o.closesAt}
              </span>
              <span className="offering-card-price">
                {t('detail.price')}: {o.price.toLocaleString()} {t('results.iqd')}
              </span>
            </div>
            {desc && <p>{desc}</p>}
            {o.videoUrl && (
              <a
                className="action-btn"
                href={o.videoUrl}
                target="_blank"
                rel="noreferrer"
                style={{ marginTop: '0.5rem', display: 'inline-flex' }}
              >
                ▶ {t('detail.watchVideo')}
              </a>
            )}
            {o.photoUrls.length > 0 && (
              <div className="offering-photos">
                {o.photoUrls.map((url) => (
                  <img key={url} src={url} alt="" loading="lazy" />
                ))}
              </div>
            )}
          </div>
        )
      })}
    </div>
  )
}
