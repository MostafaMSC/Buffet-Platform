import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { getDashboardOverview } from '../../api/endpoints'
import { EmptyState, Skeleton } from '../../components/ui'
import type { DashboardOverview } from '../../types'
import { formatDate, money } from '../../utils/format'

export function OverviewPage() {
  const { t, i18n } = useTranslation()
  const [data, setData] = useState<DashboardOverview | null>(null)
  const [failed, setFailed] = useState(false)

  useEffect(() => {
    getDashboardOverview().then(setData).catch(() => setFailed(true))
  }, [])

  if (failed) return <EmptyState icon="⚠️" title={t('common.error')} />
  if (!data) return <div className="stat-grid">{Array.from({ length: 6 }, (_, i) => <Skeleton key={i} height={92} radius={14} />)}</div>

  const peak = Math.max(1, ...data.last14Days.map((d) => d.totalPartySize))

  return (
    <div className="stack stack-5">
      <div className="section-head">
        <div>
          <h1 style={{ fontSize: '1.5rem' }}>{t('dashboard.overview')}</h1>
          <p>{formatDate(data.date, i18n.language, { weekday: 'long', day: 'numeric', month: 'long' })}</p>
        </div>
        {data.pendingRequests > 0 && (
          <Link className="btn sm" to="/dashboard/bookings?status=Pending">
            {t('dashboard.pendingRequests')} · {data.pendingRequests}
          </Link>
        )}
      </div>

      <div className="stat-grid">
        <Stat value={data.todayBookings} label={t('dashboard.todayBookings')} />
        <Stat value={data.todayGuests} label={t('dashboard.todayGuests')} />
        <Stat value={data.upcomingBookings} label={t('dashboard.upcoming')} />
        <Stat value={money(data.todayRevenue, 'IQD', i18n.language)} label={t('dashboard.todayRevenue')} />
        <Stat value={money(data.revenue30Days, 'IQD', i18n.language)} label={t('dashboard.revenue30')} />
        <Stat value={`${data.noShowRatePercent}%`} label={t('dashboard.noShowRate')} />
      </div>

      <div className="card card-pad stack stack-3">
        <div className="row-between">
          <h3>{t('dashboard.last14')}</h3>
          <span className="tiny muted">{t('dashboard.bookingsByType')}: {data.buffetBookings30Days} / {data.setMenuBookings30Days}</span>
        </div>

        {data.last14Days.every((d) => d.totalPartySize === 0) ? (
          <p className="small muted">{t('dashboard.noBookingsToday')}</p>
        ) : (
          <>
            <div className="bars" role="img" aria-label={t('dashboard.last14')}>
              {data.last14Days.map((day) => (
                <div key={day.date} className="bar" title={`${day.date}: ${day.totalPartySize}`}>
                  <span style={{ height: `${Math.round((day.totalPartySize / peak) * 100)}%` }} />
                </div>
              ))}
            </div>
            <div className="row-between tiny muted">
              <span>{formatDate(data.last14Days[0].date, i18n.language)}</span>
              <span>{formatDate(data.last14Days[data.last14Days.length - 1].date, i18n.language)}</span>
            </div>
          </>
        )}
      </div>

      {data.topServiceName && (
        <div className="card card-pad row-between wrap">
          <div className="stack" style={{ gap: 2 }}>
            <span className="eyebrow">{t('dashboard.topService')}</span>
            <strong>{i18n.language === 'ar' ? data.topServiceNameAr : data.topServiceName}</strong>
          </div>
          <span className="badge good">{data.topServiceBookings}</span>
        </div>
      )}
    </div>
  )
}

function Stat({ value, label }: { value: string | number; label: string }) {
  return (
    <div className="stat">
      <div className="value">{value}</div>
      <div className="label">{label}</div>
    </div>
  )
}
