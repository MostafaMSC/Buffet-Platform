import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { api } from '../../api/client'
import type { RestaurantSettings } from '../../types'

export function BookingSettingsPage() {
  const { t } = useTranslation()
  const [settings, setSettings] = useState<RestaurantSettings | null>(null)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [saved, setSaved] = useState(false)

  useEffect(() => {
    api.get<RestaurantSettings>('/dashboard/booking/settings').then((res) => {
      setSettings(res.data)
      setLoading(false)
    })
  }, [])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!settings) return
    setSaving(true)
    setError(null)
    setSaved(false)
    try {
      await api.put('/dashboard/booking/settings', {
        cancellationCutoffMinutes: settings.cancellationCutoffMinutes,
        waitlistOfferWindowMinutes: settings.waitlistOfferWindowMinutes,
        overbookingTolerancePercent: settings.overbookingTolerancePercent,
      })
      setSaved(true)
    } catch (err: unknown) {
      const message =
        (err as { response?: { data?: { message?: string } } })?.response?.data?.message ?? t('common.error')
      setError(message)
    } finally {
      setSaving(false)
    }
  }

  if (loading || !settings) return <p className="state-message">{t('common.loading')}</p>

  return (
    <div className="container">
      <Link to="/dashboard" className="nav-link" style={{ display: 'inline-block', marginBottom: '0.75rem' }}>
        ← {t('dashboard.title')}
      </Link>
      <form className="form-card" onSubmit={handleSubmit} style={{ maxWidth: 480 }}>
        <h1>{t('bookingSettings.title')}</h1>

        {settings.isFoundingRestaurant && <div className="badge accent">{t('bookingSettings.foundingBadge')}</div>}

        {error && <div className="form-error">{error}</div>}
        {saved && <div className="banner pending" style={{ background: '#e3f2ea', color: 'var(--color-accent)' }}>{t('bookingSettings.saved')}</div>}

        <div className="form-field">
          <label>{t('bookingSettings.cancellationCutoff')}</label>
          <input
            type="number"
            min={0}
            value={settings.cancellationCutoffMinutes}
            onChange={(e) => setSettings({ ...settings, cancellationCutoffMinutes: Number(e.target.value) })}
          />
          <span style={{ fontSize: '0.78rem', color: 'var(--color-text-muted)' }}>{t('bookingSettings.cancellationCutoffHint')}</span>
        </div>

        <div className="form-field">
          <label>{t('bookingSettings.waitlistWindow')}</label>
          <input
            type="number"
            min={1}
            value={settings.waitlistOfferWindowMinutes}
            onChange={(e) => setSettings({ ...settings, waitlistOfferWindowMinutes: Number(e.target.value) })}
          />
          <span style={{ fontSize: '0.78rem', color: 'var(--color-text-muted)' }}>{t('bookingSettings.waitlistWindowHint')}</span>
        </div>

        <div className="form-field">
          <label>{t('bookingSettings.overbooking')}</label>
          <input
            type="number"
            min={0}
            max={100}
            value={settings.overbookingTolerancePercent}
            onChange={(e) => setSettings({ ...settings, overbookingTolerancePercent: Number(e.target.value) })}
          />
          <span style={{ fontSize: '0.78rem', color: 'var(--color-text-muted)' }}>{t('bookingSettings.overbookingHint')}</span>
        </div>

        <button className="btn" type="submit" disabled={saving}>
          {t('dashboard.save')}
        </button>
      </form>
    </div>
  )
}
