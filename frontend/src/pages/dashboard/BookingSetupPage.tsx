import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router-dom'
import { api } from '../../api/client'
import type { OfferingCapacity, TimeSlot } from '../../types'

interface SlotFormState {
  id: number | null
  startTime: string
  endTime: string
  capacity: number
  bufferMinutes: number
}

const emptySlotForm: SlotFormState = { id: null, startTime: '13:00', endTime: '15:00', capacity: 50, bufferMinutes: 0 }

export function BookingSetupPage() {
  const { id } = useParams()
  const offeringId = Number(id)
  const { t } = useTranslation()

  const [data, setData] = useState<OfferingCapacity | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [wholeWindowCapacity, setWholeWindowCapacity] = useState('')
  const [slotForm, setSlotForm] = useState<SlotFormState | null>(null)
  const [savingSlotId, setSavingSlotId] = useState<number | null>(null)

  const load = () => {
    setLoading(true)
    api
      .get<OfferingCapacity>(`/dashboard/booking/offerings/${offeringId}/capacity`)
      .then((res) => {
        setData(res.data)
        setWholeWindowCapacity(res.data.capacity?.toString() ?? '')
      })
      .finally(() => setLoading(false))
  }

  useEffect(load, [offeringId])

  const extractError = (err: unknown) =>
    (err as { response?: { data?: { message?: string } } })?.response?.data?.message ?? t('common.error')

  const saveWholeWindowCapacity = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    try {
      const value = wholeWindowCapacity.trim() === '' ? null : Number(wholeWindowCapacity)
      await api.put(`/dashboard/booking/offerings/${offeringId}/capacity`, { capacity: value })
      load()
    } catch (err) {
      setError(extractError(err))
    }
  }

  const quickEditCapacity = async (slotId: number, capacity: number) => {
    setSavingSlotId(slotId)
    try {
      await api.patch(`/dashboard/booking/slots/${slotId}/capacity`, { capacity })
      load()
    } catch (err) {
      setError(extractError(err))
    } finally {
      setSavingSlotId(null)
    }
  }

  const deleteSlot = async (slotId: number) => {
    if (!confirm(t('bookingSetup.confirmDeleteSlot'))) return
    try {
      await api.delete(`/dashboard/booking/slots/${slotId}`)
      load()
    } catch (err) {
      setError(extractError(err))
    }
  }

  const startEdit = (slot?: TimeSlot) => {
    setError(null)
    setSlotForm(
      slot
        ? { id: slot.id, startTime: slot.startTime, endTime: slot.endTime, capacity: slot.capacity, bufferMinutes: slot.bufferMinutes }
        : { ...emptySlotForm },
    )
  }

  const submitSlotForm = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!slotForm) return
    setError(null)
    try {
      if (slotForm.id) {
        await api.put(`/dashboard/booking/slots/${slotForm.id}`, {
          startTime: slotForm.startTime,
          endTime: slotForm.endTime,
          capacity: slotForm.capacity,
          bufferMinutes: slotForm.bufferMinutes,
        })
      } else {
        await api.post('/dashboard/booking/slots', {
          offeringId,
          startTime: slotForm.startTime,
          endTime: slotForm.endTime,
          capacity: slotForm.capacity,
          bufferMinutes: slotForm.bufferMinutes,
        })
      }
      setSlotForm(null)
      load()
    } catch (err) {
      setError(extractError(err))
    }
  }

  if (loading) return <p className="state-message">{t('common.loading')}</p>
  if (!data) return <p className="state-message">{t('common.error')}</p>

  const hasSlots = data.slots.length > 0

  return (
    <div className="container">
      <Link to="/dashboard" className="nav-link" style={{ display: 'inline-block', marginBottom: '0.75rem' }}>
        ← {t('dashboard.title')}
      </Link>
      <h1>{t('bookingSetup.title')}</h1>
      <p style={{ color: 'var(--color-text-muted)', marginTop: '-0.5rem' }}>{t('bookingSetup.subtitle')}</p>

      {error && <div className="form-error">{error}</div>}

      {!hasSlots && (
        <div className="offering-manage-card">
          <h2 style={{ fontSize: '1rem', marginTop: 0 }}>{t('bookingSetup.wholeWindowTitle')}</h2>
          <p style={{ fontSize: '0.85rem', color: 'var(--color-text-muted)' }}>{t('bookingSetup.wholeWindowHint')}</p>
          <form onSubmit={saveWholeWindowCapacity} className="form-row" style={{ alignItems: 'flex-end' }}>
            <div className="form-field">
              <label>{t('bookingSetup.capacity')}</label>
              <input
                type="number"
                min={0}
                placeholder={t('bookingSetup.capacityPlaceholder')}
                value={wholeWindowCapacity}
                onChange={(e) => setWholeWindowCapacity(e.target.value)}
              />
            </div>
            <button className="btn small" type="submit" style={{ marginBottom: '0.9rem' }}>
              {t('dashboard.save')}
            </button>
          </form>
        </div>
      )}

      <div className="dashboard-section-header">
        <h2>{t('bookingSetup.slotsTitle')}</h2>
        <button className="btn small" onClick={() => startEdit()}>
          + {t('bookingSetup.addSlot')}
        </button>
      </div>

      {!hasSlots && <p className="state-message">{t('bookingSetup.noSlots')}</p>}

      {data.slots.map((slot) => (
        <div className="offering-manage-card" key={slot.id}>
          <div className="offering-manage-header">
            <div>
              <strong>
                {slot.startTime}–{slot.endTime}
              </strong>
              {slot.bufferMinutes > 0 && (
                <div style={{ fontSize: '0.8rem', color: 'var(--color-text-muted)' }}>
                  {t('bookingSetup.bufferLabel', { minutes: slot.bufferMinutes })}
                </div>
              )}
            </div>
            <div className="offering-manage-actions">
              <button className="btn small secondary" onClick={() => startEdit(slot)}>
                {t('dashboard.editOffering')}
              </button>
              <button className="btn small danger" onClick={() => deleteSlot(slot.id)}>
                {t('dashboard.deleteOffering')}
              </button>
            </div>
          </div>
          <div className="form-field" style={{ maxWidth: 180 }}>
            <label>{t('bookingSetup.capacity')}</label>
            <input
              type="number"
              min={1}
              defaultValue={slot.capacity}
              disabled={savingSlotId === slot.id}
              onBlur={(e) => {
                const value = Number(e.target.value)
                if (value > 0 && value !== slot.capacity) quickEditCapacity(slot.id, value)
              }}
            />
          </div>
        </div>
      ))}

      {slotForm && (
        <form className="form-card" onSubmit={submitSlotForm} style={{ maxWidth: 480, marginTop: '1rem' }}>
          <h2 style={{ fontSize: '1rem', marginTop: 0 }}>
            {slotForm.id ? t('bookingSetup.editSlot') : t('bookingSetup.addSlot')}
          </h2>
          <div className="form-row">
            <div className="form-field">
              <label>{t('offeringForm.opensAt')}</label>
              <input
                type="time"
                required
                value={slotForm.startTime}
                onChange={(e) => setSlotForm({ ...slotForm, startTime: e.target.value })}
              />
            </div>
            <div className="form-field">
              <label>{t('offeringForm.closesAt')}</label>
              <input
                type="time"
                required
                value={slotForm.endTime}
                onChange={(e) => setSlotForm({ ...slotForm, endTime: e.target.value })}
              />
            </div>
          </div>
          <div className="form-row">
            <div className="form-field">
              <label>{t('bookingSetup.capacity')}</label>
              <input
                type="number"
                min={1}
                required
                value={slotForm.capacity}
                onChange={(e) => setSlotForm({ ...slotForm, capacity: Number(e.target.value) })}
              />
            </div>
            <div className="form-field">
              <label>{t('bookingSetup.bufferMinutes')}</label>
              <input
                type="number"
                min={0}
                value={slotForm.bufferMinutes}
                onChange={(e) => setSlotForm({ ...slotForm, bufferMinutes: Number(e.target.value) })}
              />
            </div>
          </div>
          <span style={{ fontSize: '0.78rem', color: 'var(--color-text-muted)', display: 'block', marginBottom: '0.8rem' }}>
            {t('bookingSetup.bufferHint')}
          </span>
          <div style={{ display: 'flex', gap: '0.5rem' }}>
            <button className="btn" type="submit">
              {t('dashboard.save')}
            </button>
            <button className="btn secondary" type="button" onClick={() => setSlotForm(null)}>
              {t('dashboard.cancel')}
            </button>
          </div>
        </form>
      )}
    </div>
  )
}
