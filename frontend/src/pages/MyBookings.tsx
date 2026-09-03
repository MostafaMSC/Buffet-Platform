import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { api } from '../api/client'
import type { MyLookupResult } from '../types'

export function MyBookings() {
  const { t, i18n } = useTranslation()
  const isAr = i18n.language === 'ar'
  const [phone, setPhone] = useState('')
  const [result, setResult] = useState<MyLookupResult | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [confirmingId, setConfirmingId] = useState<number | null>(null)

  const search = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setLoading(true)
    try {
      const res = await api.get<MyLookupResult>('/bookings/mine', { params: { phone } })
      setResult(res.data)
    } catch {
      setError(t('common.error'))
    } finally {
      setLoading(false)
    }
  }

  const confirmOffer = async (waitlistId: number) => {
    setConfirmingId(waitlistId)
    setError(null)
    try {
      await api.post(`/bookings/waitlist/${waitlistId}/confirm`, { customerPhone: phone })
      const res = await api.get<MyLookupResult>('/bookings/mine', { params: { phone } })
      setResult(res.data)
    } catch (err) {
      setError((err as { response?: { data?: { message?: string } } })?.response?.data?.message ?? t('common.error'))
    } finally {
      setConfirmingId(null)
    }
  }

  return (
    <div className="container">
      <h1>{t('booking.myBookingsTitle')}</h1>
      <form className="form-row" onSubmit={search} style={{ alignItems: 'flex-end', maxWidth: 420 }}>
        <div className="form-field">
          <label>{t('booking.phone')}</label>
          <input required value={phone} onChange={(e) => setPhone(e.target.value)} />
        </div>
        <button className="btn" type="submit" disabled={loading}>
          {t('filters.search')}
        </button>
      </form>

      {error && <div className="form-error">{error}</div>}
      {loading && <p className="state-message">{t('common.loading')}</p>}

      {result && result.bookings.length === 0 && result.waitlistEntries.length === 0 && (
        <p className="state-message">{t('booking.noBookingsFound')}</p>
      )}

      {result && result.bookings.length > 0 && (
        <>
          <h2 className="dashboard-section-header">{t('booking.myBookingsTitle')}</h2>
          {result.bookings.map((b) => (
            <Link to={`/bookings/${b.confirmationCode}`} key={b.id} className="offering-manage-card my-booking-row">
              <div>
                <strong>{isAr ? b.restaurantNameAr : b.restaurantName}</strong>
                <div style={{ fontSize: '0.85rem', color: 'var(--color-text-muted)' }}>
                  {b.date} {b.slotStartTime ? `· ${b.slotStartTime}–${b.slotEndTime}` : ''} · {b.partySize} {t('booking.partySize')}
                </div>
              </div>
              <span className={`status-pill ${b.status === 'Confirmed' ? 'on' : 'off'}`}>{t(`bookingStatus.${b.status}`)}</span>
            </Link>
          ))}
        </>
      )}

      {result && result.waitlistEntries.length > 0 && (
        <>
          <h2 className="dashboard-section-header">{t('booking.myWaitlistTitle')}</h2>
          {result.waitlistEntries.map((w) => (
            <div className="offering-manage-card my-booking-row" key={w.id}>
              <div>
                <strong>{isAr ? w.restaurantNameAr : w.restaurantName}</strong>
                <div style={{ fontSize: '0.85rem', color: 'var(--color-text-muted)' }}>
                  {w.date} {w.slotStartTime ? `· ${w.slotStartTime}–${w.slotEndTime}` : ''} · {t('booking.waitlistPosition', { position: w.position })}
                </div>
              </div>
              {w.status === 'Offered' ? (
                <button className="btn small" disabled={confirmingId === w.id} onClick={() => confirmOffer(w.id)}>
                  {t('booking.confirmOffer')}
                </button>
              ) : (
                <span className="status-pill off">{t(`waitlistStatus.${w.status}`)}</span>
              )}
            </div>
          ))}
        </>
      )}
    </div>
  )
}
