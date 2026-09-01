import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { api } from '../api/client'
import type { RestaurantAdminListItem } from '../types'

export function AdminDashboard() {
  const { t } = useTranslation()
  const [restaurants, setRestaurants] = useState<RestaurantAdminListItem[]>([])
  const [loading, setLoading] = useState(true)

  const load = () => {
    setLoading(true)
    api
      .get<RestaurantAdminListItem[]>('/admin/restaurants')
      .then((res) => setRestaurants(res.data))
      .finally(() => setLoading(false))
  }

  useEffect(load, [])

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
    </div>
  )
}
