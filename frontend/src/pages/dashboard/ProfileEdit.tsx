import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { api } from '../../api/client'
import { PhotoUploader } from '../../components/PhotoUploader'
import { AreaSelect } from '../../components/AreaSelect'
import { Skeleton } from '../../components/ui'
import type { RestaurantProfile } from '../../types'

/// The venue behind the services: how it is named, where it is, and how a guest reaches it.
export function ProfileEdit() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const [form, setForm] = useState<RestaurantProfile | null>(null)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState(false)

  useEffect(() => {
    api.get<RestaurantProfile>('/dashboard/profile').then(
      (profileRes) => {
        setForm(profileRes.data)
      },
    )
  }, [])

  if (!form) return <Skeleton height={480} radius={14} />

  const update = <K extends keyof RestaurantProfile>(key: K, value: RestaurantProfile[K]) =>
    setForm((prev) => (prev ? { ...prev, [key]: value } : prev))

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setSaving(true)
    setError(false)
    try {
      await api.put('/dashboard/profile', {
        name: form.name,
        nameAr: form.nameAr,
        areaId: form.areaId,
        phoneNumber: form.phoneNumber,
        address: form.address,
        googleMapsUrl: form.googleMapsUrl,
        description: form.description,
        descriptionAr: form.descriptionAr,
        logoUrl: form.logoUrl,
        coverPhotoUrl: form.coverPhotoUrl,
      })
      navigate('/dashboard')
    } catch {
      setError(true)
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="stack stack-5">
      <div className="section-head">
        <div><h1 style={{ fontSize: '1.5rem' }}>{t('dashboard.editProfile')}</h1></div>
      </div>

      {error && <div className="alert bad">{t('common.error')}</div>}

      <form className="card card-pad stack stack-4" onSubmit={handleSubmit} style={{ maxWidth: 620 }}>
        <div className="row wrap" style={{ gap: 'var(--sp-3)' }}>
          <label className="field grow" style={{ minWidth: 220 }}>
            <span>{t('profileForm.name')}</span>
            <input required value={form.name} onChange={(e) => update('name', e.target.value)} />
          </label>
          <label className="field grow" style={{ minWidth: 220 }}>
            <span>{t('profileForm.nameAr')}</span>
            <input
              required
              dir="rtl"
              value={form.nameAr}
              onChange={(e) => update('nameAr', e.target.value)}
            />
          </label>
        </div>

        <label className="field">
          <span>{t('profileForm.area')}</span>
          <AreaSelect required value={form.areaId} onChange={(id) => update('areaId', id)} />
        </label>

        <label className="field">
          <span>{t('profileForm.phone')}</span>
          <input
            required
            value={form.phoneNumber}
            onChange={(e) => update('phoneNumber', e.target.value)}
          />
        </label>

        <label className="field">
          <span>{t('profileForm.address')}</span>
          <input value={form.address ?? ''} onChange={(e) => update('address', e.target.value)} />
        </label>

        <label className="field">
          <span>{t('profileForm.googleMapsUrl')}</span>
          <input
            value={form.googleMapsUrl ?? ''}
            onChange={(e) => update('googleMapsUrl', e.target.value)}
          />
        </label>

        <label className="field">
          <span>{t('profileForm.description')}</span>
          <textarea
            rows={2}
            value={form.description ?? ''}
            onChange={(e) => update('description', e.target.value)}
          />
        </label>

        <label className="field">
          <span>{t('profileForm.descriptionAr')}</span>
          <textarea
            rows={2}
            dir="rtl"
            value={form.descriptionAr ?? ''}
            onChange={(e) => update('descriptionAr', e.target.value)}
          />
        </label>

        <label className="field">
          <span>{t('profileForm.logo')}</span>
          <PhotoUploader
            urls={form.logoUrl ? [form.logoUrl] : []}
            maxPhotos={1}
            onChange={(urls) => update('logoUrl', urls[0] ?? null)}
          />
        </label>

        <label className="field">
          <span>{t('profileForm.cover')}</span>
          <PhotoUploader
            urls={form.coverPhotoUrl ? [form.coverPhotoUrl] : []}
            maxPhotos={1}
            onChange={(urls) => update('coverPhotoUrl', urls[0] ?? null)}
          />
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
