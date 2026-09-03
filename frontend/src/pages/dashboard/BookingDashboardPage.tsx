import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { api } from '../../api/client'
import type { BookingAnalytics, RestaurantBookingGroup } from '../../types'

function todayBaghdad() {
  const now = new Date()
  const baghdad = new Date(now.getTime() + (3 * 60 - now.getTimezoneOffset()) * 60000)
  return baghdad.toISOString().slice(0, 10)
}

function daysAgo(n: number) {
  const d = new Date(todayBaghdad())
  d.setDate(d.getDate() - n)
  return d.toISOString().slice(0, 10)
}

export function BookingDashboardPage() {
  const { t } = useTranslation()
  const [date, setDate] = useState(todayBaghdad())
  const [groups, setGroups] = useState<RestaurantBookingGroup[]>([])
  const [loading, setLoading] = useState(true)
  const [busyId, setBusyId] = useState<number | null>(null)
  const [analytics, setAnalytics] = useState<BookingAnalytics | null>(null)

  const loadBookings = () => {
    setLoading(true)
    api
      .get<RestaurantBookingGroup[]>('/dashboard/bookings', { params: { date } })
      .then((res) => setGroups(res.data))
      .finally(() => setLoading(false))
  }

  useEffect(loadBookings, [date])

  useEffect(() => {
    api
      .get<BookingAnalytics>('/dashboard/bookings/analytics', { params: { start: daysAgo(29), end: todayBaghdad() } })
      .then((res) => setAnalytics(res.data))
  }, [])

  const markStatus = async (bookingId: number, status: 'NoShow' | 'Completed') => {
    setBusyId(bookingId)
    try {
      await api.patch(`/dashboard/bookings/${bookingId}/status`, { status })
      loadBookings()
    } finally {
      setBusyId(null)
    }
  }

  return (
    <div className="container">
      <Link to="/dashboard" className="nav-link" style={{ display: 'inline-block', marginBottom: '0.75rem' }}>
        ← {t('dashboard.title')}
      </Link>
      <h1>{t('bookingDashboard.title')}</h1>

      {analytics && (
        <div className="analytics-cards">
          <div className="analytics-card">
            <span className="analytics-value">{analytics.totalBookings}</span>
            <span className="analytics-label">{t('bookingDashboard.totalBookings')}</span>
          </div>
          <div className="analytics-card">
            <span className="analytics-value">{analytics.completedCount}</span>
            <span className="analytics-label">{t('bookingDashboard.completed')}</span>
          </div>
          <div className="analytics-card">
            <span className="analytics-value">{analytics.noShowCount}</span>
            <span className="analytics-label">{t('bookingDashboard.noShows')}</span>
          </div>
          <div className="analytics-card">
            <span className="analytics-value">{analytics.noShowRatePercent}%</span>
            <span className="analytics-label">{t('bookingDashboard.noShowRate')}</span>
          </div>
        </div>
      )}

      <div className="form-field" style={{ maxWidth: 220, marginTop: '1.25rem' }}>
        <label>{t('filters.date')}</label>
        <input type="date" value={date} onChange={(e) => setDate(e.target.value)} />
      </div>

      {loading && <p className="state-message">{t('common.loading')}</p>}
      {!loading && groups.length === 0 && <p className="state-message">{t('bookingDashboard.noBookings')}</p>}

      {groups.map((g) => (
        <div className="offering-manage-card" key={`${g.offeringId}-${g.timeSlotId ?? 'whole'}`}>
          <div className="offering-manage-header">
            <div>
              <span className="badge">{t(`mealType.${g.mealType}`)}</span>{' '}
              <strong>
                {g.startTime}–{g.endTime}
              </strong>
              <div style={{ fontSize: '0.85rem', color: 'var(--color-text-muted)' }}>
                {t('bookingDashboard.capacityLine', { booked: g.bookedPartySize, capacity: g.effectiveCapacity })}
              </div>
            </div>
          </div>

          {g.bookings.map((b) => (
            <div className="booking-row" key={b.id}>
              <div>
                <strong>{b.customerName}</strong>
                <div style={{ fontSize: '0.8rem', color: 'var(--color-text-muted)' }}>
                  {b.customerPhone} · {b.partySize} {t('booking.partySize')} · {b.confirmationCode}
                </div>
              </div>
              <div style={{ display: 'flex', gap: '0.4rem', alignItems: 'center' }}>
                {b.status === 'Confirmed' ? (
                  <>
                    <button className="btn small secondary" disabled={busyId === b.id} onClick={() => markStatus(b.id, 'Completed')}>
                      {t('bookingDashboard.markCompleted')}
                    </button>
                    <button className="btn small danger" disabled={busyId === b.id} onClick={() => markStatus(b.id, 'NoShow')}>
                      {t('bookingDashboard.markNoShow')}
                    </button>
                  </>
                ) : (
                  <span className={`status-pill ${b.status === 'Completed' ? 'on' : 'off'}`}>{t(`bookingStatus.${b.status}`)}</span>
                )}
              </div>
            </div>
          ))}
        </div>
      ))}
    </div>
  )
}
