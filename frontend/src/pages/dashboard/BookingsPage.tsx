import { useCallback, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router-dom'
import { getRestaurantBookings, markBookingStatus } from '../../api/endpoints'
import { Badge, EmptyState, Select, Skeleton } from '../../components/ui'
import type { BookingStatus, RestaurantBookingGroup } from '../../types'
import { apiError, formatDate, formatTime, money, todayInBaghdad } from '../../utils/format'

/// Which actions make sense for a booking in each state — the same transitions the API
/// enforces, so a button never appears that the server would refuse.
const ACTIONS: Record<string, { status: BookingStatus; labelKey: string; variant?: string }[]> = {
  Pending: [
    { status: 'Confirmed', labelKey: 'bookingsAdmin.confirm', variant: 'success' },
    { status: 'Rejected', labelKey: 'bookingsAdmin.reject', variant: 'ghost' },
  ],
  Confirmed: [
    { status: 'CheckedIn', labelKey: 'bookingsAdmin.checkIn', variant: 'secondary' },
    { status: 'Completed', labelKey: 'bookingsAdmin.complete', variant: 'secondary' },
    { status: 'NoShow', labelKey: 'bookingsAdmin.noShow', variant: 'ghost' },
  ],
  CheckedIn: [
    { status: 'Completed', labelKey: 'bookingsAdmin.complete', variant: 'success' },
    { status: 'NoShow', labelKey: 'bookingsAdmin.noShow', variant: 'ghost' },
  ],
}

const STATUS_KIND: Record<string, 'good' | 'warn' | 'bad' | undefined> = {
  Confirmed: 'good', CheckedIn: 'good', Completed: 'good',
  Pending: 'warn', Cancelled: 'bad', Rejected: 'bad', NoShow: 'bad',
}

export function BookingsPage() {
  const { t, i18n } = useTranslation()
  const [params, setParams] = useSearchParams()

  const date = params.get('date') ?? todayInBaghdad()
  const status = params.get('status') ?? ''

  const [groups, setGroups] = useState<RestaurantBookingGroup[] | null>(null)
  const [busyId, setBusyId] = useState<number | null>(null)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(() => {
    setGroups(null)
    getRestaurantBookings(date, status || undefined)
      .then(setGroups)
      .catch(() => setGroups([]))
  }, [date, status])

  useEffect(load, [load])

  const act = async (bookingId: number, next: BookingStatus) => {
    setBusyId(bookingId)
    setError(null)
    try {
      await markBookingStatus(bookingId, next)
      load()
    } catch (err) {
      setError(apiError(err, t('common.error'), t))
    } finally {
      setBusyId(null)
    }
  }

  const patch = (changes: Record<string, string>) => {
    const next = new URLSearchParams(params)
    Object.entries(changes).forEach(([k, v]) => (v ? next.set(k, v) : next.delete(k)))
    setParams(next)
  }

  return (
    <div className="stack stack-5">
      <div className="section-head">
        <div>
          <h1 style={{ fontSize: '1.5rem' }}>{t('bookingsAdmin.title')}</h1>
          <p>{t('bookingsAdmin.subtitle')}</p>
        </div>
      </div>

      <div className="row wrap" style={{ gap: 'var(--sp-3)' }}>
        <label className="field" style={{ width: 190 }}>
          <span>{t('bookingsAdmin.date')}</span>
          <input type="date" value={date} onChange={(e) => patch({ date: e.target.value })} />
        </label>
        <label className="field" style={{ width: 190 }}>
          <span>{t('bookingsAdmin.status')}</span>
          <Select value={status} onChange={(e) => patch({ status: e.target.value })}>
            <option value="">{t('bookingsAdmin.all')}</option>
            {(['Pending', 'Confirmed', 'CheckedIn', 'Completed', 'NoShow', 'Cancelled'] as const).map((s) => (
              <option key={s} value={s}>{t(`bookingStatus.${s}`)}</option>
            ))}
          </Select>
        </label>
      </div>

      {error && <div className="alert bad">{error}</div>}

      {!groups && <Skeleton height={220} radius={14} />}

      {groups?.length === 0 && <EmptyState icon="📋" title={t('bookingsAdmin.empty')} />}

      {groups?.map((group) => {
        const fillPercent = Math.min(100, Math.round((group.bookedPartySize / Math.max(1, group.effectiveCapacity)) * 100))
        return (
          <div className="card stack" key={`${group.serviceId}-${group.timeSlotId ?? 'w'}-${group.date}`}>
            <div className="card-pad-sm stack stack-2" style={{ borderBottom: '1px solid var(--c-line)' }}>
              <div className="row-between wrap">
                <div className="stack" style={{ gap: 2 }}>
                  <strong>{i18n.language === 'ar' ? group.serviceNameAr : group.serviceName}</strong>
                  <span className="tiny muted">
                    {formatDate(group.date, i18n.language)} · {formatTime(group.startTime, i18n.language)}–{formatTime(group.endTime, i18n.language)}
                  </span>
                </div>
                <span className="small strong nums">
                  {t('bookingsAdmin.fill', { booked: group.bookedPartySize, capacity: group.effectiveCapacity })}
                </span>
              </div>
              <div className={`meter ${fillPercent >= 100 ? 'full' : fillPercent >= 80 ? 'high' : ''}`}>
                <span style={{ width: `${fillPercent}%` }} />
              </div>
            </div>

            <div className="table-wrap" style={{ border: 0, borderRadius: 0 }}>
              <table className="data">
                <thead>
                  <tr>
                    <th>{t('bookingsAdmin.guest')}</th>
                    <th>{t('bookingsAdmin.party')}</th>
                    <th>{t('bookingsAdmin.value')}</th>
                    <th>{t('bookingsAdmin.ref')}</th>
                    <th>{t('bookingsAdmin.status')}</th>
                    <th>{t('bookingsAdmin.actions')}</th>
                  </tr>
                </thead>
                <tbody>
                  {group.bookings.map((booking) => (
                    <tr key={booking.id}>
                      <td>
                        <div className="strong">{booking.customerName}</div>
                        <div className="tiny muted">{booking.customerPhone}</div>
                        {booking.specialRequests && <div className="tiny" style={{ color: 'var(--c-warn)' }}>{booking.specialRequests}</div>}
                      </td>
                      <td className="nums">{booking.adults}+{booking.children}</td>
                      <td className="nums">{money(booking.totalPrice, 'IQD', i18n.language)}</td>
                      <td className="nums tiny">{booking.confirmationCode}</td>
                      <td><Badge kind={STATUS_KIND[booking.status]}>{t(`bookingStatus.${booking.status}`)}</Badge></td>
                      <td>
                        <div className="row" style={{ gap: 4 }}>
                          {(ACTIONS[booking.status] ?? []).map((action) => (
                            <button
                              key={action.status}
                              className={`btn sm ${action.variant ?? 'secondary'}`}
                              disabled={busyId === booking.id}
                              onClick={() => act(booking.id, action.status)}
                            >
                              {t(action.labelKey)}
                            </button>
                          ))}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )
      })}
    </div>
  )
}
