import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { api } from '../../api/client'
import { Badge, Skeleton } from '../../components/ui'
import type { RestaurantSettings } from '../../types'
import { apiError } from '../../utils/format'

/// The rules that sit behind every booking this restaurant takes: how late a guest may
/// cancel, how far ahead the waitlist opens, and how far past capacity it will go.
export function BookingSettingsPage() {
  const { t } = useTranslation()
  const [settings, setSettings] = useState<RestaurantSettings | null>(null)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [saved, setSaved] = useState(false)

  useEffect(() => {
    api.get<RestaurantSettings>('/dashboard/booking/settings').then((res) => setSettings(res.data))
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
    } catch (err) {
      setError(apiError(err, t('common.error'), t))
    } finally {
      setSaving(false)
    }
  }

  if (!settings) return <Skeleton height={320} radius={14} />

  const update = <K extends keyof RestaurantSettings>(key: K, value: RestaurantSettings[K]) =>
    setSettings((prev) => (prev ? { ...prev, [key]: value } : prev))

  return (
    <div className="stack stack-5">
      <div className="section-head">
        <div>
          <h1 style={{ fontSize: '1.5rem' }}>{t('bookingSettings.title')}</h1>
          {settings.isFoundingRestaurant && <Badge kind="solid">{t('bookingSettings.foundingBadge')}</Badge>}
        </div>
      </div>

      {error && <div className="alert bad">{error}</div>}
      {saved && <div className="alert good">{t('bookingSettings.saved')}</div>}

      <form className="card card-pad stack stack-4" onSubmit={handleSubmit} style={{ maxWidth: 520 }}>
        <label className="field">
          <span>{t('bookingSettings.cancellationCutoff')}</span>
          <input
            type="number"
            min={0}
            value={settings.cancellationCutoffMinutes}
            onChange={(e) => update('cancellationCutoffMinutes', Number(e.target.value))}
          />
          <span className="hint">{t('bookingSettings.cancellationCutoffHint')}</span>
        </label>

        <label className="field">
          <span>{t('bookingSettings.waitlistWindow')}</span>
          <input
            type="number"
            min={1}
            value={settings.waitlistOfferWindowMinutes}
            onChange={(e) => update('waitlistOfferWindowMinutes', Number(e.target.value))}
          />
          <span className="hint">{t('bookingSettings.waitlistWindowHint')}</span>
        </label>

        <label className="field">
          <span>{t('bookingSettings.overbooking')}</span>
          <input
            type="number"
            min={0}
            max={100}
            value={settings.overbookingTolerancePercent}
            onChange={(e) => update('overbookingTolerancePercent', Number(e.target.value))}
          />
          <span className="hint">{t('bookingSettings.overbookingHint')}</span>
        </label>

        <div>
          <button className="btn" type="submit" disabled={saving}>
            {saving ? t('common.loading') : t('common.save')}
          </button>
        </div>
      </form>
    </div>
  )
}
