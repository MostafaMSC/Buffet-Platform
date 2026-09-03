import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { api } from '../api/client'
import type { BookingAvailability, BookingDetail, SlotAvailability, WaitlistDetail } from '../types'

function todayBaghdad() {
  const now = new Date()
  const baghdad = new Date(now.getTime() + (3 * 60 - now.getTimezoneOffset()) * 60000)
  return baghdad.toISOString().slice(0, 10)
}

export function BookingWidget({ offeringId }: { offeringId: number }) {
  const { t } = useTranslation()
  const [date, setDate] = useState(todayBaghdad())
  const [availability, setAvailability] = useState<BookingAvailability | null>(null)
  const [loading, setLoading] = useState(true)
  const [selectedSlotId, setSelectedSlotId] = useState<number | null | undefined>(undefined)
  const [name, setName] = useState('')
  const [phone, setPhone] = useState('')
  const [partySize, setPartySize] = useState(2)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [bookingResult, setBookingResult] = useState<BookingDetail | null>(null)
  const [waitlistResult, setWaitlistResult] = useState<WaitlistDetail | null>(null)

  useEffect(() => {
    setLoading(true)
    setAvailability(null)
    setSelectedSlotId(undefined)
    setError(null)
    api
      .get<BookingAvailability>('/bookings/availability', { params: { offeringId, date } })
      .then((res) => {
        setAvailability(res.data)
        if (res.data.slots.length === 1) setSelectedSlotId(res.data.slots[0].timeSlotId)
      })
      .finally(() => setLoading(false))
  }, [offeringId, date])

  const extractError = (err: unknown) =>
    (err as { response?: { data?: { message?: string } } })?.response?.data?.message ?? t('common.error')

  const selectedSlot: SlotAvailability | undefined = availability?.slots.find((s) => s.timeSlotId === selectedSlotId)

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setSubmitting(true)
    const payload = {
      offeringId,
      timeSlotId: selectedSlotId ?? null,
      date,
      customerName: name,
      customerPhone: phone,
      partySize,
    }
    try {
      if (selectedSlot?.isFull) {
        const res = await api.post<WaitlistDetail>('/bookings/waitlist', payload)
        setWaitlistResult(res.data)
      } else {
        const res = await api.post<BookingDetail>('/bookings', payload)
        setBookingResult(res.data)
      }
    } catch (err) {
      setError(extractError(err))
    } finally {
      setSubmitting(false)
    }
  }

  if (loading && !availability) return null

  if (bookingResult) {
    return (
      <div className="booking-widget booking-success">
        <p>{t('booking.successBooked')}</p>
        <div className="booking-code">{bookingResult.confirmationCode}</div>
        <Link className="btn small" to={`/bookings/${bookingResult.confirmationCode}`}>
          {t('booking.viewBadge')}
        </Link>
      </div>
    )
  }

  if (waitlistResult) {
    return (
      <div className="booking-widget booking-success">
        <p>{t('booking.successWaitlisted', { position: waitlistResult.position })}</p>
        <Link className="btn small" to="/my-bookings">
          {t('booking.viewMyBookings')}
        </Link>
      </div>
    )
  }

  return (
    <div className="booking-widget">
      <h3 className="booking-widget-title">{t('booking.title')}</h3>
      <div className="form-field">
        <label>{t('filters.date')}</label>
        <input type="date" min={todayBaghdad()} value={date} onChange={(e) => setDate(e.target.value)} />
      </div>

      {loading && <p className="state-message">{t('common.loading')}</p>}

      {availability && availability.slots.length === 0 && <p className="state-message">{t('booking.notAvailable')}</p>}

      {availability && availability.slots.length > 0 && (
        <>
          <div className="slot-picker">
            {availability.slots.map((slot) => (
              <button
                type="button"
                key={slot.timeSlotId ?? 'whole'}
                className={`slot-option ${selectedSlotId === slot.timeSlotId ? 'active' : ''} ${slot.isFull ? 'full' : ''}`}
                onClick={() => setSelectedSlotId(slot.timeSlotId)}
              >
                <span>
                  {slot.startTime}–{slot.endTime}
                </span>
                <span className="slot-option-status">
                  {slot.isFull
                    ? t('booking.full', { count: slot.waitlistLength })
                    : t('booking.remaining', { count: slot.remaining })}
                </span>
              </button>
            ))}
          </div>

          {selectedSlotId !== undefined && (
            <form onSubmit={submit} className="booking-form">
              {error && <div className="form-error">{error}</div>}
              <div className="form-row">
                <div className="form-field">
                  <label>{t('booking.name')}</label>
                  <input required value={name} onChange={(e) => setName(e.target.value)} />
                </div>
                <div className="form-field">
                  <label>{t('booking.phone')}</label>
                  <input required value={phone} onChange={(e) => setPhone(e.target.value)} />
                </div>
                <div className="form-field" style={{ maxWidth: 120 }}>
                  <label>{t('booking.partySize')}</label>
                  <input
                    type="number"
                    min={1}
                    required
                    value={partySize}
                    onChange={(e) => setPartySize(Number(e.target.value))}
                  />
                </div>
              </div>
              <button className="btn" type="submit" disabled={submitting}>
                {selectedSlot?.isFull ? t('booking.joinWaitlist') : t('booking.confirmBooking')}
              </button>
            </form>
          )}
        </>
      )}
    </div>
  )
}
