import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { api } from '../api/client'
import { Badge, EmptyState, Select, Skeleton } from '../components/ui'
import type { AdminRestaurantSettings, PlatformBookingStats, RestaurantAdminListItem } from '../types'
import { todayInBaghdad } from '../utils/format'

function daysAgo(n: number) {
  const d = new Date(`${todayInBaghdad()}T00:00:00`)
  d.setDate(d.getDate() - n)
  return d.toISOString().slice(0, 10)
}

interface SettingsEdit {
  overbookingTolerancePercent: number
  isFoundingRestaurant: boolean
  featuredScore: number
  referredByRestaurantId: number | null
}

/// Platform moderation: who is waiting to be let in, who is live, and the per-restaurant
/// levers (overbooking tolerance, founding badge, featured weight, referral) that the
/// platform — not the restaurant — controls.
export function AdminDashboard() {
  const { t, i18n } = useTranslation()
  const isAr = i18n.language === 'ar'

  const [restaurants, setRestaurants] = useState<RestaurantAdminListItem[] | null>(null)
  const [bookingSettings, setBookingSettings] = useState<AdminRestaurantSettings[]>([])
  const [edits, setEdits] = useState<Record<number, SettingsEdit>>({})
  const [savingId, setSavingId] = useState<number | null>(null)
  const [stats, setStats] = useState<PlatformBookingStats | null>(null)

  const load = () => {
    api
      .get<RestaurantAdminListItem[]>('/admin/restaurants')
      .then((res) => setRestaurants(res.data))
      .catch(() => setRestaurants([]))
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
      .get<PlatformBookingStats>('/admin/bookings/stats', { params: { start: daysAgo(29), end: todayInBaghdad() } })
      .then((res) => setStats(res.data))
      .catch(() => setStats(null))
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

  const statusKind = (status: string) =>
    status === 'Approved' ? 'good' : status === 'Pending' ? 'warn' : 'bad'

  const pending = restaurants?.filter((r) => r.status === 'Pending') ?? []

  return (
    <div className="container section-tight stack stack-6">
      <div className="section-head">
        <div>
          <h1 style={{ fontSize: '1.5rem' }}>{t('admin.title')}</h1>
          <p>{t('admin.all')}</p>
        </div>
      </div>

      {stats && (
        <div className="stat-grid">
          <div className="stat">
            <div className="value nums">{stats.totalBookings}</div>
            <div className="label">{t('admin.statsTitle')}</div>
          </div>
          <div className="stat">
            <div className="value nums">{stats.totalPartySize}</div>
            <div className="label">{t('admin.totalGuests')}</div>
          </div>
          <div className="stat">
            <div className="value nums">{stats.restaurantsWithBookings}</div>
            <div className="label">{t('admin.activeRestaurants')}</div>
          </div>
        </div>
      )}

      <section className="stack stack-3">
        <div className="section-head"><h2>{t('admin.pending')}</h2></div>

        {!restaurants && <Skeleton height={90} radius={14} />}

        {restaurants && pending.length === 0 && (
          <EmptyState icon="✅" title={t('admin.noPending')} />
        )}

        {pending.length > 0 && (
          <div className="table-wrap">
            <table className="data">
              <thead>
                <tr>
                  <th>{t('auth.restaurantName')}</th>
                  <th>{t('auth.area')}</th>
                  <th>{t('auth.phone')}</th>
                  <th>{t('admin.services')}</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {pending.map((r) => (
                  <tr key={r.id}>
                    <td>
                      <strong>{isAr ? r.nameAr : r.name}</strong>
                    </td>
                    <td>{r.areaNameEn}</td>
                    <td className="nums">{r.phoneNumber}</td>
                    <td className="nums">{r.serviceCount}</td>
                    <td>
                      <div className="row" style={{ gap: 'var(--sp-2)' }}>
                        <button className="btn sm" onClick={() => act(r.id, 'approve')}>{t('admin.approve')}</button>
                        <button className="btn quiet-danger sm" onClick={() => act(r.id, 'reject')}>{t('admin.reject')}</button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <section className="stack stack-3">
        <div className="section-head"><h2>{t('admin.all')}</h2></div>

        {!restaurants && <Skeleton height={220} radius={14} />}

        {restaurants && restaurants.length > 0 && (
          <div className="table-wrap">
            <table className="data">
              <thead>
                <tr>
                  <th>{t('auth.restaurantName')}</th>
                  <th>{t('auth.area')}</th>
                  <th>{t('auth.phone')}</th>
                  <th>{t('bookingsAdmin.status')}</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {restaurants.map((r) => (
                  <tr key={r.id}>
                    <td>{isAr ? r.nameAr : r.name}</td>
                    <td>{r.areaNameEn}</td>
                    <td className="nums">{r.phoneNumber}</td>
                    <td>
                      <Badge kind={statusKind(r.status)}>{t(`status.${r.status}`)}</Badge>
                    </td>
                    <td>
                      <div className="row" style={{ gap: 'var(--sp-2)' }}>
                        {r.status === 'Approved' && (
                          <button className="btn quiet-danger sm" onClick={() => act(r.id, 'suspend')}>{t('admin.suspend')}</button>
                        )}
                        {(r.status === 'Suspended' || r.status === 'Rejected') && (
                          <button className="btn secondary sm" onClick={() => act(r.id, 'reinstate')}>{t('admin.reinstate')}</button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <section className="stack stack-3">
        <div className="section-head"><h2>{t('admin.settingsTitle')}</h2></div>

        <div className="table-wrap">
          <table className="data">
            <thead>
              <tr>
                <th>{t('auth.restaurantName')}</th>
                <th>{t('admin.overbooking')}</th>
                <th>{t('admin.founding')}</th>
                <th>{t('admin.featuredScore')}</th>
                <th>{t('admin.referredBy')}</th>
                <th />
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
                        style={{ width: 76 }}
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
                        style={{ width: 76 }}
                        value={edit.featuredScore}
                        onChange={(e) => setEdits({ ...edits, [s.restaurantId]: { ...edit, featuredScore: Number(e.target.value) } })}
                      />
                    </td>
                    <td>
                      <Select
                        value={edit.referredByRestaurantId ?? ''}
                        onChange={(e) =>
                          setEdits({
                            ...edits,
                            [s.restaurantId]: { ...edit, referredByRestaurantId: e.target.value === '' ? null : Number(e.target.value) },
                          })
                        }
                      >
                        <option value="">{t('common.none')}</option>
                        {bookingSettings
                          .filter((other) => other.restaurantId !== s.restaurantId)
                          .map((other) => (
                            <option key={other.restaurantId} value={other.restaurantId}>
                              {other.restaurantName}
                            </option>
                          ))}
                      </Select>
                    </td>
                    <td>
                      <button className="btn sm" disabled={savingId === s.restaurantId} onClick={() => saveSettings(s.restaurantId)}>
                        {t('common.save')}
                      </button>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  )
}
