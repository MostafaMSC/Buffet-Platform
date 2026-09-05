import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams, useSearchParams } from 'react-router-dom'
import { cancelBooking, getBooking, submitReview } from '../api/endpoints'
import { Badge, EmptyState, Icon, Skeleton, StarRatingInput } from '../components/ui'
import type { BookingDetail } from '../types'
import { apiError, formatDateLong, formatTime, money } from '../utils/format'

const STATUS_KIND: Record<string, 'good' | 'warn' | 'bad' | undefined> = {
  Confirmed: 'good',
  CheckedIn: 'good',
  Completed: 'good',
  Pending: 'warn',
  Waitlisted: 'warn',
  Cancelled: 'bad',
  Rejected: 'bad',
  NoShow: 'bad',
}

/// The booking itself: a reference the guest shows at the door, the details, and the one
/// action they own — cancelling within the restaurant's cutoff.
export function BookingDetailPage() {
  const { code } = useParams()
  const { t, i18n } = useTranslation()
  const [params] = useSearchParams()
  const justBooked = params.get('new') === '1'

  const [booking, setBooking] = useState<BookingDetail | null>(null)
  const [notFound, setNotFound] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const [reviewRating, setReviewRating] = useState(0)
  const [reviewComment, setReviewComment] = useState('')
  const [reviewBusy, setReviewBusy] = useState(false)
  const [reviewError, setReviewError] = useState<string | null>(null)

  const load = () => {
    setNotFound(false)
    getBooking(code!)
      .then(setBooking)
      .catch(() => setNotFound(true))
  }

  useEffect(load, [code])

  const cancel = async () => {
    if (!confirm(t('booking.cancelConfirm'))) return
    setBusy(true)
    setError(null)
    try {
      await cancelBooking(code!)
      load()
    } catch (err) {
      setError(apiError(err, t('common.error'), t))
    } finally {
      setBusy(false)
    }
  }

  const submitBookingReview = async () => {
    setReviewBusy(true)
    setReviewError(null)
    try {
      await submitReview(code!, reviewRating, reviewComment.trim())
      load()
    } catch (err) {
      setReviewError(apiError(err, t('common.error'), t))
    } finally {
      setReviewBusy(false)
    }
  }

  if (notFound) {
    return (
      <div className="container section">
        <EmptyState
          icon="🔎"
          title={t('booking.notFound')}
          actions={<Link className="btn secondary" to="/my-bookings">{t('booking.lookupTitle')}</Link>}
        />
      </div>
    )
  }

  if (!booking) {
    return (
      <div className="container section" style={{ maxWidth: 560 }}>
        <Skeleton height={220} radius={20} />
      </div>
    )
  }

  const isAr = i18n.language === 'ar'
  const canCancel = booking.status === 'Confirmed' || booking.status === 'Pending'

  return (
    <div className="container container-narrow section">
      {justBooked && (
        <div className="stack stack-2" style={{ textAlign: 'center', marginBottom: 'var(--sp-5)' }}>
          <span style={{ fontSize: '2.2rem' }} aria-hidden>{booking.status === 'Pending' ? '⏳' : '🎉'}</span>
          <h1>{booking.status === 'Pending' ? t('booking.pendingTitle') : t('booking.successTitle')}</h1>
          <p className="soft">{booking.status === 'Pending' ? t('booking.pendingText') : t('booking.successText')}</p>
        </div>
      )}

      <div className="panel stack stack-5">
        <div className="row-between wrap">
          <Badge kind={STATUS_KIND[booking.status]}>{t(`bookingStatus.${booking.status}`)}</Badge>
          <span className="tiny muted">{t('booking.bookedOn', { date: new Date(booking.createdAt).toLocaleDateString(isAr ? 'ar-IQ' : 'en-GB') })}</span>
        </div>

        <div style={{ textAlign: 'center' }}>
          <div className="eyebrow">{t('booking.reference')}</div>
          <div className="confirm-code">{booking.confirmationCode}</div>
        </div>

        <div className="card card-pad stack stack-3">
          <div className="row" style={{ gap: 'var(--sp-4)' }}>
            {booking.photoUrl && (
              <img
                src={booking.photoUrl}
                alt=""
                style={{ width: 76, height: 76, borderRadius: 'var(--r-sm)', objectFit: 'cover' }}
              />
            )}
            <div className="stack" style={{ gap: 2 }}>
              <strong>{isAr ? booking.serviceNameAr : booking.serviceName}</strong>
              <span className="small soft">{isAr ? booking.restaurantNameAr : booking.restaurantName}</span>
              <span className="tiny muted">
                {isAr ? booking.areaNameAr : booking.areaName}, {isAr ? booking.cityNameAr : booking.cityName}
              </span>
            </div>
          </div>

          <div className="divider" style={{ margin: 0 }} />

          <div className="price-row">
            <span className="soft"><Icon name="calendar" size={15} /> {t('booking.date')}</span>
            <span className="strong">{formatDateLong(booking.date, i18n.language)}</span>
          </div>
          {booking.slotStartTime && (
            <div className="price-row">
              <span className="soft"><Icon name="clock" size={15} /> {t('booking.time')}</span>
              <span className="strong">
                {formatTime(booking.slotStartTime, i18n.language)} – {formatTime(booking.slotEndTime ?? '', i18n.language)}
              </span>
            </div>
          )}
          <div className="price-row">
            <span className="soft"><Icon name="users" size={15} /> {t('booking.guests')}</span>
            <span className="strong">{t('booking.guestsLine', { adults: booking.adults, children: booking.children })}</span>
          </div>
          <div className="price-row">
            <span className="soft">{t('booking.name')}</span>
            <span className="strong">{booking.customerName}</span>
          </div>
          {booking.specialRequests && (
            <div className="price-row">
              <span className="soft">{t('booking.requests')}</span>
              <span>{booking.specialRequests}</span>
            </div>
          )}
          <div className="price-row total">
            <span>{t('price.total')}</span>
            <span className="nums">{money(booking.totalPrice, booking.currencyCode, i18n.language)}</span>
          </div>
        </div>

        {booking.status === 'Completed' && (
          <div className="card card-pad stack stack-3">
            {booking.hasReview ? (
              <p className="small soft">{t('booking.rateThanks')}</p>
            ) : (
              <>
                <strong>{t('booking.rateTitle')}</strong>
                <StarRatingInput value={reviewRating} onChange={setReviewRating} />
                <textarea
                  className="input"
                  rows={3}
                  placeholder={t('booking.rateCommentPlaceholder') ?? undefined}
                  value={reviewComment}
                  onChange={(e) => setReviewComment(e.target.value)}
                />
                {reviewError && <div className="alert bad">{reviewError}</div>}
                <button
                  className="btn"
                  disabled={reviewRating === 0 || reviewBusy}
                  onClick={submitBookingReview}
                >
                  {reviewBusy ? t('booking.rateSubmitting') : t('booking.rateSubmit')}
                </button>
              </>
            )}
          </div>
        )}

        {error && <div className="alert bad">{error}</div>}

        <div className="row wrap" style={{ gap: 'var(--sp-2)' }}>
          <a className="btn secondary" href={`tel:${booking.restaurantPhone}`}>{t('booking.contactRestaurant')}</a>
          <Link className="btn secondary" to={`/services/${booking.serviceId}`}>{t('detail.menu')}</Link>
          {canCancel && (
            <button className="btn danger" onClick={cancel} disabled={busy}>{t('booking.cancelBooking')}</button>
          )}
        </div>

        {/* The cancellation window is only meaningful while the booking can still be cancelled. */}
        {canCancel && (
          <p className="tiny muted">{t('detail.cancellation', { minutes: booking.cancellationCutoffMinutes })}</p>
        )}
      </div>

      <div className="row" style={{ justifyContent: 'center', marginTop: 'var(--sp-5)' }}>
        <Link className="link" to="/my-bookings">{t('booking.lookupTitle')}</Link>
      </div>
    </div>
  )
}
