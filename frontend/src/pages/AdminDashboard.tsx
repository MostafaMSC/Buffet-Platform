import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { api } from '../api/client'
import type { AdminRestaurantSettings, PlatformBookingStats, RestaurantAdminListItem } from '../types'

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

interface SettingsEdit {
  overbookingTolerancePercent: number
  isFoundingRestaurant: boolean
  featuredScore: number
  referredByRestaurantId: number | null
}

export function AdminDashboard() {
  const { t } = useTranslation()
  const [restaurants, setRestaurants] = useState<RestaurantAdminListItem[]>([])
  const [loading, setLoading] = useState(true)
  const [bookingSettings, setBookingSettings] = useState<AdminRestaurantSettings[]>([])
  const [edits, setEdits] = useState<Record<number, SettingsEdit>>({})
  const [savingId, setSavingId] = useState<number | null>(null)
  const [platformStats, setPlatformStats] = useState<PlatformBookingStats | null>(null)

  const load = () => {
    setLoading(true)
    api
      .get<RestaurantAdminListItem[]>('/admin/restaurants')
      .then((res) => setRestaurants(res.data))
      .finally(() => setLoading(false))
  }

  const loadBookingSettings = () => {
    api.get<AdminRestaurantSettings[]>('/admin/booking-settings').then((res) => {
      setBookingSettings(res.data)
      setEdits(
        Object.fromEntries(
          res.data.map((s) => [
            s.restaurantId,
            {
              overbookingTolerancePercent: s.overbookingTolerancePercent,
              isFoundingRestaurant: s.isFoundingRestaurant,
              featuredScore: s.featuredScore,
              referredByRestaurantId: s.referredByRestaurantId,
            },
          ]),
        ),
      )
    })
  }

  useEffect(load, [])
  useEffect(loadBookingSettings, [])
  useEffect(() => {
    api
      .get<PlatformBookingStats>('/admin/bookings/stats', { params: { start: daysAgo(29), end: todayBaghdad() } })
      .then((res) => setPlatformStats(res.data))
  }, [])

  const saveSettings = async (restaurantId: number) => {
    const edit = edits[restaurantId]
    if (!edit) return
    setSavingId(restaurantId)
    try {
      await api.put(`/admin/booking-settings/${restaurantId}`, edit)
      loadBookingSettings()
    } finally {
      setSavingId(null)
    }
  }

  const act = async (id: number, action: 'approve' | 'reject' | 'suspend' | 'reinstate') => {
    await api.post(`/admin/restaurants/${id}/${action}`)
    load()
  }

  if (loading) return <p className="state-message">{t('common.loading')}</p>

  const pending = restaurants.filter((r) => r.status === 'Pending')

  return (
    <div className="container">
      <h1>{t('admin.title')}</h1>

      <div className="dashboard-section-header">
        <h2>{t('admin.pending')}</h2>
      </div>

      {pending.length === 0 && <p className="state-message">{t('admin.noPending')}</p>}

      {pending.length > 0 && (
        <div className="admin-table-wrap" style={{ marginBottom: '1.5rem' }}>
          <table className="admin-table">
            <tbody>
              {pending.map((r) => (
                <tr key={r.id}>
                  <td>
                    <strong>{r.name}</strong>
                    <div style={{ fontSize: '0.8rem', color: 'var(--color-text-muted)' }}>{r.nameAr}</div>
                  </td>
                  <td>{r.areaNameEn}</td>
                  <td>{r.phoneNumber}</td>
                  <td>
                    {r.offeringCount} {t('admin.offerings')}
                  </td>
                  <td>
                    <div className="table-actions">
                      <button className="btn small" onClick={() => act(r.id, 'approve')}>
                        {t('admin.approve')}
                      </button>
                      <button className="btn small danger" onClick={() => act(r.id, 'reject')}>
                        {t('admin.reject')}
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <div className="dashboard-section-header">
        <h2>{t('admin.all')}</h2>
      </div>

      <div className="admin-table-wrap">
        <table className="admin-table">
          <thead>
            <tr>
              <th>{t('auth.restaurantName')}</th>
              <th>{t('filters.area')}</th>
              <th>{t('auth.phone')}</th>
              <th>{t('status.Approved')}</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {restaurants.map((r) => (
              <tr key={r.id}>
                <td>{r.name}</td>
                <td>{r.areaNameEn}</td>
                <td>{r.phoneNumber}</td>
                <td>
                  <span className={`status-tag ${r.status}`}>{t(`status.${r.status}`)}</span>
                </td>
                <td>
                  <div className="table-actions">
                    {r.status === 'Approved' && (
                      <button className="btn small danger" onClick={() => act(r.id, 'suspend')}>
                        {t('admin.suspend')}
                      </button>
                    )}
                    {(r.status === 'Suspended' || r.status === 'Rejected') && (
                      <button className="btn small" onClick={() => act(r.id, 'reinstate')}>
                        {t('admin.reinstate')}
                      </button>
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {platformStats && (
        <>
          <div className="dashboard-section-header">
            <h2>{t('adminBooking.statsTitle')}</h2>
          </div>
          <div className="analytics-cards" style={{ marginBottom: '1.5rem' }}>
            <div className="analytics-card">
              <span className="analytics-value">{platformStats.totalBookings}</span>
              <span className="analytics-label">{t('bookingDashboard.totalBookings')}</span>
            </div>
            <div className="analytics-card">
              <span className="analytics-value">{platformStats.totalPartySize}</span>
              <span className="analytics-label">{t('adminBooking.totalPartySize')}</span>
            </div>
            <div className="analytics-card">
              <span className="analytics-value">{platformStats.restaurantsWithBookings}</span>
              <span className="analytics-label">{t('adminBooking.restaurantsWithBookings')}</span>
            </div>
          </div>
        </>
      )}

      <div className="dashboard-section-header">
        <h2>{t('adminBooking.settingsTitle')}</h2>
      </div>
      <div className="admin-table-wrap">
        <table className="admin-table">
          <thead>
            <tr>
              <th>{t('auth.restaurantName')}</th>
              <th>{t('bookingSettings.overbooking')}</th>
              <th>{t('bookingSettings.foundingBadge')}</th>
              <th>{t('adminBooking.featuredScore')}</th>
              <th>{t('adminBooking.referredBy')}</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {bookingSettings.map((s) => {
              const edit = edits[s.restaurantId]
              if (!edit) return null
              return (
                <tr key={s.restaurantId}>
                  <td>{s.restaurantName}</td>
                  <td>
                    <input
                      type="number"
                      min={0}
                      max={100}
                      style={{ width: 70 }}
                      value={edit.overbookingTolerancePercent}
                      onChange={(e) =>
                        setEdits({ ...edits, [s.restaurantId]: { ...edit, overbookingTolerancePercent: Number(e.target.value) } })
                      }
                    />
                  </td>
                  <td>
                    <input
                      type="checkbox"
                      checked={edit.isFoundingRestaurant}
                      onChange={(e) => setEdits({ ...edits, [s.restaurantId]: { ...edit, isFoundingRestaurant: e.target.checked } })}
                    />
                  </td>
                  <td>
                    <input
                      type="number"
                      min={0}
                      style={{ width: 70 }}
                      value={edit.featuredScore}
                      onChange={(e) => setEdits({ ...edits, [s.restaurantId]: { ...edit, featuredScore: Number(e.target.value) } })}
                    />
                  </td>
                  <td>
                    <select
                      value={edit.referredByRestaurantId ?? ''}
                      onChange={(e) =>
                        setEdits({
                          ...edits,
                          [s.restaurantId]: { ...edit, referredByRestaurantId: e.target.value === '' ? null : Number(e.target.value) },
                        })
                      }
                    >
                      <option value="">—</option>
                      {bookingSettings
                        .filter((other) => other.restaurantId !== s.restaurantId)
                        .map((other) => (
                          <option key={other.restaurantId} value={other.restaurantId}>
                            {other.restaurantName}
                          </option>
                        ))}
                    </select>
                  </td>
                  <td>
                    <button className="btn small" disabled={savingId === s.restaurantId} onClick={() => saveSettings(s.restaurantId)}>
                      {t('dashboard.save')}
                    </button>
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>
    </div>
  )
}
