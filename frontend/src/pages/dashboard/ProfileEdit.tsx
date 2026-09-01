import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { api } from '../../api/client'
import { PhotoUploader } from '../../components/PhotoUploader'
import type { Area, RestaurantProfile } from '../../types'

export function ProfileEdit() {
  const { t, i18n } = useTranslation()
  const isAr = i18n.language === 'ar'
  const navigate = useNavigate()

  const [areas, setAreas] = useState<Area[]>([])
  const [form, setForm] = useState<RestaurantProfile | null>(null)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState(false)

  useEffect(() => {
    Promise.all([api.get<Area[]>('/areas'), api.get<RestaurantProfile>('/dashboard/profile')]).then(
      ([areasRes, profileRes]) => {
        setAreas(areasRes.data)
        setForm(profileRes.data)
      },
    )
  }, [])

  if (!form) return <p className="state-message">{t('common.loading')}</p>

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
    <div className="container">
      <form className="form-card" onSubmit={handleSubmit} style={{ maxWidth: 560 }}>
        <h1>{t('dashboard.editProfile')}</h1>
        {error && <div className="form-error">{t('common.error')}</div>}

        <div className="form-row">
          <div className="form-field">
            <label>{t('profileForm.name')}</label>
            <input required value={form.name} onChange={(e) => update('name', e.target.value)} />
          </div>
          <div className="form-field">
            <label>{t('profileForm.nameAr')}</label>
            <input
              required
              dir="rtl"
              value={form.nameAr}
              onChange={(e) => update('nameAr', e.target.value)}
            />
          </div>
        </div>

        <div className="form-field">
          <label>{t('profileForm.area')}</label>
          <select required value={form.areaId} onChange={(e) => update('areaId', Number(e.target.value))}>
            {areas.map((a) => (
              <option key={a.id} value={a.id}>
                {isAr ? a.nameAr : a.nameEn}
              </option>
            ))}
          </select>
        </div>

        <div className="form-field">
          <label>{t('profileForm.phone')}</label>
          <input
            required
            value={form.phoneNumber}
            onChange={(e) => update('phoneNumber', e.target.value)}
          />
        </div>

        <div className="form-field">
          <label>{t('profileForm.address')}</label>
          <input value={form.address ?? ''} onChange={(e) => update('address', e.target.value)} />
        </div>

        <div className="form-field">
          <label>{t('profileForm.googleMapsUrl')}</label>
          <input
            value={form.googleMapsUrl ?? ''}
            onChange={(e) => update('googleMapsUrl', e.target.value)}
          />
        </div>

        <div className="form-field">
          <label>{t('profileForm.description')}</label>
          <textarea
            rows={2}
            value={form.description ?? ''}
            onChange={(e) => update('description', e.target.value)}
          />
        </div>

        <div className="form-field">
          <label>{t('profileForm.descriptionAr')}</label>
          <textarea
            rows={2}
            dir="rtl"
            value={form.descriptionAr ?? ''}
            onChange={(e) => update('descriptionAr', e.target.value)}
          />
        </div>

        <div className="form-field">
          <label>{t('profileForm.logo')}</label>
          <PhotoUploader
            urls={form.logoUrl ? [form.logoUrl] : []}
            maxPhotos={1}
            onChange={(urls) => update('logoUrl', urls[0] ?? null)}
          />
        </div>

        <div className="form-field">
          <label>{t('profileForm.cover')}</label>
          <PhotoUploader
            urls={form.coverPhotoUrl ? [form.coverPhotoUrl] : []}
            maxPhotos={1}
            onChange={(urls) => update('coverPhotoUrl', urls[0] ?? null)}
          />
        </div>

        <button className="btn" type="submit" disabled={saving}>
          {t('dashboard.save')}
        </button>
      </form>
    </div>
  )
}
