import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate, useParams } from 'react-router-dom'
import { api } from '../../api/client'
import { PhotoUploader } from '../../components/PhotoUploader'
import type { DashboardOffering, MealType, OfferingInput, RecurrenceType, WeekdayName } from '../../types'

const MEAL_TYPES: MealType[] = ['Breakfast', 'Lunch', 'Iftar', 'Sohor']
const RECURRENCE_TYPES: RecurrenceType[] = ['Daily', 'SpecificWeekdays', 'RamadanMode', 'OneOff']
const WEEKDAYS: WeekdayName[] = [
  'Monday',
  'Tuesday',
  'Wednesday',
  'Thursday',
  'Friday',
  'Saturday',
  'Sunday',
]

const emptyForm: OfferingInput = {
  mealType: 'Breakfast',
  price: 0,
  opensAt: '08:00',
  closesAt: '11:00',
  description: '',
  descriptionAr: '',
  recurrence: 'Daily',
  weekdays: [],
  ramadanStartDate: null,
  ramadanEndDate: null,
  oneOffDate: null,
  photoUrls: [],
}

export function OfferingFormPage() {
  const { id } = useParams()
  const isEdit = Boolean(id)
  const { t } = useTranslation()
  const navigate = useNavigate()

  const [form, setForm] = useState<OfferingInput>(emptyForm)
  const [loading, setLoading] = useState(isEdit)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState(false)

  useEffect(() => {
    if (!isEdit) return
    api.get<DashboardOffering[]>('/dashboard/offerings', { params: { days: 1 } }).then((res) => {
      const existing = res.data.find((o) => o.id === Number(id))
      if (existing) {
        setForm({
          mealType: existing.mealType,
          price: existing.price,
          opensAt: existing.opensAt,
          closesAt: existing.closesAt,
          description: existing.description ?? '',
          descriptionAr: existing.descriptionAr ?? '',
          recurrence: existing.recurrence,
          weekdays: existing.weekdays,
          ramadanStartDate: existing.ramadanStartDate,
          ramadanEndDate: existing.ramadanEndDate,
          oneOffDate: existing.oneOffDate,
          photoUrls: existing.photoUrls,
        })
      }
      setLoading(false)
    })
  }, [id, isEdit])

  const update = <K extends keyof OfferingInput>(key: K, value: OfferingInput[K]) =>
    setForm((prev) => ({ ...prev, [key]: value }))

  const toggleWeekday = (day: WeekdayName) => {
    const current = form.weekdays ?? []
    update(
      'weekdays',
      current.includes(day) ? current.filter((d) => d !== day) : [...current, day],
    )
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setSaving(true)
    setError(false)
    try {
      if (isEdit) {
        await api.put(`/dashboard/offerings/${id}`, form)
      } else {
        await api.post('/dashboard/offerings', form)
      }
      navigate('/dashboard')
    } catch {
      setError(true)
    } finally {
      setSaving(false)
    }
  }

  if (loading) return <p className="state-message">{t('common.loading')}</p>

  return (
    <div className="container">
      <form className="form-card" onSubmit={handleSubmit} style={{ maxWidth: 560 }}>
        <h1>{t('offeringForm.title')}</h1>
        {error && <div className="form-error">{t('common.error')}</div>}

        <div className="form-row">
          <div className="form-field">
            <label>{t('offeringForm.mealType')}</label>
            <select value={form.mealType} onChange={(e) => update('mealType', e.target.value as MealType)}>
              {MEAL_TYPES.map((m) => (
                <option key={m} value={m}>
                  {t(`mealType.${m}`)}
                </option>
              ))}
            </select>
          </div>
          <div className="form-field">
            <label>{t('offeringForm.price')}</label>
            <input
              type="number"
              min={0}
              required
              value={form.price}
              onChange={(e) => update('price', Number(e.target.value))}
            />
          </div>
        </div>

        <div className="form-row">
          <div className="form-field">
            <label>{t('offeringForm.opensAt')}</label>
            <input
              type="time"
              required
              value={form.opensAt}
              onChange={(e) => update('opensAt', e.target.value)}
            />
          </div>
          <div className="form-field">
            <label>{t('offeringForm.closesAt')}</label>
            <input
              type="time"
              required
              value={form.closesAt}
              onChange={(e) => update('closesAt', e.target.value)}
            />
          </div>
        </div>

        <div className="form-field">
          <label>{t('offeringForm.description')}</label>
          <textarea
            rows={2}
            value={form.description ?? ''}
            onChange={(e) => update('description', e.target.value)}
          />
        </div>

        <div className="form-field">
          <label>{t('offeringForm.descriptionAr')}</label>
          <textarea
            rows={2}
            dir="rtl"
            value={form.descriptionAr ?? ''}
            onChange={(e) => update('descriptionAr', e.target.value)}
          />
        </div>

        <div className="form-field">
          <label>{t('offeringForm.recurrence')}</label>
          <select
            value={form.recurrence}
            onChange={(e) => update('recurrence', e.target.value as RecurrenceType)}
          >
            {RECURRENCE_TYPES.map((r) => (
              <option key={r} value={r}>
                {t(`recurrence.${r}`)}
              </option>
            ))}
          </select>
        </div>

        {form.recurrence === 'SpecificWeekdays' && (
          <div className="form-field">
            <label>{t('offeringForm.weekdays')}</label>
            <div className="checkbox-row">
              {WEEKDAYS.map((day) => (
                <button
                  type="button"
                  key={day}
                  className={`checkbox-chip ${form.weekdays?.includes(day) ? 'active' : ''}`}
                  onClick={() => toggleWeekday(day)}
                >
                  {t(`weekday.${day}`)}
                </button>
              ))}
            </div>
          </div>
        )}

        {form.recurrence === 'RamadanMode' && (
          <div className="form-row">
            <div className="form-field">
              <label>{t('offeringForm.ramadanStart')}</label>
              <input
                type="date"
                required
                value={form.ramadanStartDate ?? ''}
                onChange={(e) => update('ramadanStartDate', e.target.value)}
              />
            </div>
            <div className="form-field">
              <label>{t('offeringForm.ramadanEnd')}</label>
              <input
                type="date"
                required
                value={form.ramadanEndDate ?? ''}
                onChange={(e) => update('ramadanEndDate', e.target.value)}
              />
            </div>
          </div>
        )}

        {form.recurrence === 'OneOff' && (
          <div className="form-field">
            <label>{t('offeringForm.oneOffDate')}</label>
            <input
              type="date"
              required
              value={form.oneOffDate ?? ''}
              onChange={(e) => update('oneOffDate', e.target.value)}
            />
          </div>
        )}

        <div className="form-field">
          <label>{t('offeringForm.photos')}</label>
          <PhotoUploader urls={form.photoUrls ?? []} onChange={(urls) => update('photoUrls', urls)} />
        </div>

        <button className="btn" type="submit" disabled={saving}>
          {t('dashboard.save')}
        </button>
      </form>
    </div>
  )
}
