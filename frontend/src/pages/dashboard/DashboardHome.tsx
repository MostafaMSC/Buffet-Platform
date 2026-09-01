import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { api } from '../../api/client'
import type { DashboardOffering, RestaurantProfile } from '../../types'

export function DashboardHome() {
  const { t, i18n } = useTranslation()
  const isAr = i18n.language === 'ar'
  const [profile, setProfile] = useState<RestaurantProfile | null>(null)
  const [offerings, setOfferings] = useState<DashboardOffering[]>([])
  const [loading, setLoading] = useState(true)

  const load = () => {
    setLoading(true)
    Promise.all([
      api.get<RestaurantProfile>('/dashboard/profile'),
      api.get<DashboardOffering[]>('/dashboard/offerings', { params: { days: 7 } }),
    ])
      .then(([profileRes, offeringsRes]) => {
        setProfile(profileRes.data)
        setOfferings(offeringsRes.data)
      })
      .finally(() => setLoading(false))
  }

  useEffect(load, [])

  const toggle = async (offeringId: number, date: string, current: boolean) => {
    setOfferings((prev) =>
      prev.map((o) =>
        o.id === offeringId
          ? { ...o, days: o.days.map((d) => (d.date === date ? { ...d, isActive: !current } : d)) }
          : o,
      ),
    )
    try {
      await api.post('/dashboard/availability/toggle', { offeringId, date, isActive: !current })
    } catch {
      load()
    }
  }

  const deleteOffering = async (id: number) => {
    if (!confirm(t('dashboard.confirmDelete'))) return
    await api.delete(`/dashboard/offerings/${id}`)
    setOfferings((prev) => prev.filter((o) => o.id !== id))
  }

  if (loading) return <p className="state-message">{t('common.loading')}</p>
  if (!profile) return <p className="state-message">{t('common.error')}</p>

  return (
    <div className="container">
      <h1>{t('dashboard.title')}</h1>

      {profile.status === 'Pending' && <div className="banner pending">{t('dashboard.pendingBanner')}</div>}
      {profile.status === 'Suspended' && (
        <div className="banner suspended">{t('dashboard.suspendedBanner')}</div>
      )}

      <div className="offering-manage-card">
        <div className="offering-manage-header">
          <div>
            <strong>{isAr ? profile.nameAr : profile.name}</strong>
            <div style={{ fontSize: '0.85rem', color: 'var(--color-text-muted)' }}>{profile.areaNameEn}</div>
          </div>
          <Link className="btn small secondary" to="/dashboard/profile">
            {t('dashboard.editProfile')}
          </Link>
        </div>
      </div>

      <div className="dashboard-section-header">
        <h2>{t('dashboard.offerings')}</h2>
        <Link className="btn small" to="/dashboard/offerings/new">
          + {t('dashboard.addOffering')}
        </Link>
      </div>

      {offerings.length === 0 && <p className="state-message">{t('dashboard.noOfferings')}</p>}

      {offerings.map((o) => {
        const desc = isAr ? o.descriptionAr : o.description
        return (
          <div className="offering-manage-card" key={o.id}>
            <div className="offering-manage-header">
              <div>
                <span className="badge">{t(`mealType.${o.mealType}`)}</span>{' '}
                <strong>{o.price.toLocaleString()} {t('results.iqd')}</strong>
                <div style={{ fontSize: '0.85rem', color: 'var(--color-text-muted)' }}>
                  {o.opensAt}–{o.closesAt} · {t(`recurrence.${o.recurrence}`)}
                </div>
                {desc && <div style={{ fontSize: '0.85rem', marginTop: '0.25rem' }}>{desc}</div>}
              </div>
              <div className="offering-manage-actions">
                <Link className="btn small secondary" to={`/dashboard/offerings/${o.id}/edit`}>
                  {t('dashboard.editOffering')}
                </Link>
                <button className="btn small danger" onClick={() => deleteOffering(o.id)}>
                  {t('dashboard.deleteOffering')}
                </button>
              </div>
            </div>

            <div className="day-toggle-row">
              {o.days.map((d) => {
                const dayLabel = new Date(d.date).toLocaleDateString(isAr ? 'ar' : 'en', {
                  weekday: 'short',
                  day: 'numeric',
                })
                return (
                  <button
                    key={d.date}
                    className={`day-toggle ${d.isActive ? 'on' : 'off'}`}
                    onClick={() => toggle(o.id, d.date, d.isActive)}
                  >
                    <span className="day-label">{dayLabel}</span>
                    {d.isActive ? t('dashboard.on') : t('dashboard.off')}
                  </button>
                )
              })}
            </div>
          </div>
        )
      })}
    </div>
  )
}
