import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router-dom'
import { api } from '../api/client'
import type { BookingDetail } from '../types'

/// The "badge" a customer shows restaurant staff at the door — public by design (the
/// confirmation code in the URL is the only credential, standing in for a customer
/// account per the Phase 2 no-accounts decision).
export function BookingBadge() {
  const { code } = useParams()
  const { t, i18n } = useTranslation()
  const isAr = i18n.language === 'ar'
  const [booking, setBooking] = useState<BookingDetail | null>(null)
  const [loading, setLoading] = useState(true)
  const [notFound, setNotFound] = useState(false)
  const [cancelling, setCancelling] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const load = () => {
    setLoading(true)
    setNotFound(false)
    api
      .get<BookingDetail>(`/bookings/${code}`)
      .then((res) => setBooking(res.data))
      .catch(() => setNotFound(true))
      .finally(() => setLoading(false))
  }

  useEffect(load, [code])

  const cancel = async () => {
    if (!confirm(t('booking.confirmCancel'))) return
    setError(null)
    setCancelling(true)
    try {
      await api.post(`/bookings/${code}/cancel`)
      load()
    } catch (err) {
      setError((err as { response?: { data?: { message?: string } } })?.response?.data?.message ?? t('common.error'))
    } finally {
      setCancelling(false)
    }
  }

  if (loading) return <p className="state-message">{t('common.loading')}</p>
  if (notFound || !booking) return <p className="state-message">{t('booking.notFound')}</p>

  const restaurantName = isAr ? booking.restaurantNameAr : booking.restaurantName

  return (
    <div className="container">
      <div className="form-card booking-badge">
        <span className={`status-pill ${booking.status === 'Confirmed' ? 'on' : 'off'}`}>
          {t(`bookingStatus.${booking.status}`)}
        </span>
        <h1>{restaurantName}</h1>
        <div className="booking-code large">{booking.confirmationCode}</div>
        <dl className="booking-badge-details">
          <dt>{t('booking.mealType')}</dt>
          <dd>{t(`mealType.${booking.mealType}`)}</dd>
          <dt>{t('filters.date')}</dt>
          <dd>{booking.date}</dd>
          {booking.slotStartTime && (
            <>
              <dt>{t('detail.hours')}</dt>
              <dd>
                {booking.slotStartTime}–{booking.slotEndTime}
              </dd>
            </>
          )}
          <dt>{t('booking.name')}</dt>
          <dd>{booking.customerName}</dd>
          <dt>{t('booking.partySize')}</dt>
          <dd>{booking.partySize}</dd>
        </dl>

        {error && <div className="form-error">{error}</div>}

        {booking.status === 'Confirmed' && (
          <button className="btn danger" onClick={cancel} disabled={cancelling}>
            {t('booking.cancelBooking')}
          </button>
        )}

        <Link to="/my-bookings" className="nav-link" style={{ display: 'inline-block', marginTop: '1rem' }}>
          {t('booking.viewMyBookings')}
        </Link>
      </div>
    </div>
  )
}
