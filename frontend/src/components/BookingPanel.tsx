import { useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { createBooking, getServiceAvailability, joinWaitlist } from '../api/endpoints'
import type { ServiceAvailability, ServiceDetail, ServiceSlot } from '../types'
import { apiError, formatTime, money, todayInBaghdad } from '../utils/format'
import { Sheet, Stepper } from './ui'

type Step = 'when' | 'details' | 'review'

/// The reserve flow: pick a date and sitting, say who is coming, check the price, confirm.
/// It stays on the detail page so the customer never loses sight of what they are booking.
export function BookingPanel({
  detail,
  initialDate,
  initialGuests,
}: {
  detail: ServiceDetail
  initialDate?: string
  initialGuests?: number
}) {
  const { t, i18n } = useTranslation()
  const navigate = useNavigate()

  const [date, setDate] = useState(initialDate ?? detail.availability.date ?? todayInBaghdad())
  const [adults, setAdults] = useState(Math.max(detail.minGuests, initialGuests ?? 2))
  const [children, setChildren] = useState(0)
  const [slotId, setSlotId] = useState<number | null | undefined>(undefined)
  const [availability, setAvailability] = useState<ServiceAvailability>(detail.availability)
  const [loadingSlots, setLoadingSlots] = useState(false)

  const [open, setOpen] = useState(false)
  const [step, setStep] = useState<Step>('when')
  const [name, setName] = useState('')
  const [phone, setPhone] = useState('')
  const [email, setEmail] = useState('')
  const [requests, setRequests] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [waitlisted, setWaitlisted] = useState<number | null>(null)

  const guests = adults + children

  useEffect(() => {
    let cancelled = false
    setLoadingSlots(true)
    getServiceAvailability(detail.id, date, guests)
      .then((data) => {
        if (cancelled) return
        setAvailability(data)
        // Keep a chosen sitting only while it still exists and still fits the party.
        setSlotId((current) => {
          const match = data.slots.find((s) => s.timeSlotId === current)
          if (match && match.fitsParty && !match.isPast) return current
          const first = data.slots.find((s) => s.fitsParty && !s.isPast)
          return first ? first.timeSlotId : undefined
        })
      })
      .catch(() => { if (!cancelled) setAvailability({ ...detail.availability, slots: [] }) })
      .finally(() => { if (!cancelled) setLoadingSlots(false) })

    return () => { cancelled = true }
  }, [detail.id, date, guests, detail.availability])

  const selectedSlot: ServiceSlot | undefined = availability.slots.find((s) => s.timeSlotId === slotId)

  const quote = useMemo(() => {
    if (detail.pricingModel === 'PerPackage') {
      const per = detail.packageGuests && detail.packageGuests > 0 ? detail.packageGuests : 1
      const packages = Math.max(1, Math.ceil(guests / per))
      return { packages, total: (detail.packagePrice ?? 0) * packages, adultsTotal: 0, childrenTotal: 0 }
    }
    const childPrice = detail.pricePerChild ?? detail.pricePerAdult
    return {
      packages: null,
      adultsTotal: detail.pricePerAdult * adults,
      childrenTotal: childPrice * children,
      total: detail.pricePerAdult * adults + childPrice * children,
    }
  }, [detail, adults, children, guests])

  const canBook = availability.isServedOnDate && availability.bookingEnabled && !!selectedSlot && selectedSlot.fitsParty
  const isFullDay = availability.isServedOnDate && availability.slots.length > 0 && availability.slots.every((s) => s.isFull || s.isPast)

  const submit = async () => {
    setSubmitting(true)
    setError(null)
    try {
      const booking = await createBooking({
        serviceId: detail.id,
        timeSlotId: slotId ?? null,
        date,
        customerName: name,
        customerPhone: phone,
        adults,
        children,
        customerEmail: email || null,
        specialRequests: requests || null,
      })
      setOpen(false)
      navigate(`/bookings/${booking.confirmationCode}?new=1`)
    } catch (err) {
      setError(apiError(err, t('common.error')))
      setStep('when')
    } finally {
      setSubmitting(false)
    }
  }

  const addToWaitlist = async () => {
    setSubmitting(true)
    setError(null)
    try {
      const entry = await joinWaitlist({
        serviceId: detail.id,
        timeSlotId: slotId ?? null,
        date,
        customerName: name,
        customerPhone: phone,
        partySize: guests,
      }) as { position: number }
      setWaitlisted(entry.position)
    } catch (err) {
      setError(apiError(err, t('common.error')))
    } finally {
      setSubmitting(false)
    }
  }

  const priceHeadline = detail.pricingModel === 'PerPackage'
    ? money(detail.packagePrice ?? 0, detail.currencyCode, i18n.language)
    : money(detail.pricePerAdult, detail.currencyCode, i18n.language)

  return (
    <>
      <aside className="booking-panel stack stack-4">
        <div className="row-between" style={{ alignItems: 'baseline' }}>
          <div>
            <span style={{ fontSize: '1.3rem', fontWeight: 800 }}>{priceHeadline}</span>
            <span className="small muted">
              {' '}
              {detail.pricingModel === 'PerPackage'
                ? t('price.forGuests', { guests: detail.packageGuests ?? 2 })
                : `/ ${t('price.person')}`}
            </span>
          </div>
        </div>

        <label className="field">
          <span>{t('booking.date')}</span>
          <input type="date" min={todayInBaghdad()} value={date} onChange={(e) => setDate(e.target.value)} />
        </label>

        <div className="row-between">
          <span className="small strong">{t('price.adults')}</span>
          <Stepper value={adults} onChange={setAdults} min={1} max={detail.maxGuests ?? 40} label={t('price.adults')} />
        </div>

        {detail.pricePerChild !== null && (
          <div className="row-between">
            <div className="stack" style={{ gap: 0 }}>
              <span className="small strong">{t('price.children')}</span>
              {detail.childAgeFrom && detail.childAgeTo && (
                <span className="tiny muted">{t('price.childAges', { from: detail.childAgeFrom, to: detail.childAgeTo })}</span>
              )}
            </div>
            <Stepper value={children} onChange={setChildren} min={0} max={detail.maxGuests ?? 20} label={t('price.children')} />
          </div>
        )}

        {!availability.isServedOnDate && (
          <div className="alert warn">{t('availability.notServed')}</div>
        )}

        {availability.isServedOnDate && !availability.bookingEnabled && (
          <div className="alert info">{t('availability.walkIn')}</div>
        )}

        {availability.isServedOnDate && availability.bookingEnabled && (
          <div className="stack stack-2">
            <span className="small strong">{t('booking.selectSlot')}</span>
            {loadingSlots ? (
              <div className="skeleton" style={{ height: 58 }} />
            ) : availability.slots.length === 0 ? (
              <p className="small muted">{t('booking.noSlots')}</p>
            ) : (
              <div className="slot-grid">
                {availability.slots.map((slot) => (
                  <button
                    key={slot.timeSlotId ?? 'window'}
                    type="button"
                    className={`slot-option ${slotId === slot.timeSlotId ? 'active' : ''}`}
                    disabled={slot.isPast || slot.isFull}
                    onClick={() => setSlotId(slot.timeSlotId)}
                  >
                    <span className="time">{formatTime(slot.startTime, i18n.language)}</span>
                    <span className="left">
                      {slot.isPast
                        ? t('availability.started')
                        : slot.isFull
                          ? t('availability.full')
                          : t('availability.seatsLeft', { count: slot.remaining })}
                    </span>
                  </button>
                ))}
              </div>
            )}
          </div>
        )}

        <div className="stack stack-1">
          {detail.pricingModel === 'PerPackage' ? (
            <div className="price-row">
              <span className="soft">
                {t('price.packagesLine', { count: quote.packages ?? 1, price: money(detail.packagePrice ?? 0, detail.currencyCode, i18n.language) })}
              </span>
              <span className="nums">{money(quote.total, detail.currencyCode, i18n.language)}</span>
            </div>
          ) : (
            <>
              <div className="price-row">
                <span className="soft">{t('price.adultsLine', { count: adults, price: money(detail.pricePerAdult, detail.currencyCode, i18n.language) })}</span>
                <span className="nums">{money(quote.adultsTotal, detail.currencyCode, i18n.language)}</span>
              </div>
              {children > 0 && (
                <div className="price-row">
                  <span className="soft">
                    {t('price.childrenLine', { count: children, price: money(detail.pricePerChild ?? detail.pricePerAdult, detail.currencyCode, i18n.language) })}
                  </span>
                  <span className="nums">{money(quote.childrenTotal, detail.currencyCode, i18n.language)}</span>
                </div>
              )}
            </>
          )}
          <div className="price-row total">
            <span>{t('price.total')}</span>
            <span className="nums">{money(quote.total, detail.currencyCode, i18n.language)}</span>
          </div>
        </div>

        {error && <div className="alert bad">{error}</div>}

        {isFullDay && !canBook ? (
          <button className="btn secondary block" onClick={() => { setStep('details'); setOpen(true) }}>
            {t('booking.joinWaitlist')}
          </button>
        ) : (
          <button className="btn block lg" disabled={!canBook} onClick={() => { setStep('details'); setOpen(true) }}>
            {detail.bookingMode === 'Request' ? t('booking.request') : t('booking.title')}
          </button>
        )}

        <p className="tiny muted" style={{ textAlign: 'center' }}>
          {t('detail.cancellation', { minutes: detail.cancellationCutoffMinutes })}
        </p>
      </aside>

      {open && (
        <Sheet
          title={detail.bookingMode === 'Request' ? t('booking.request') : t('booking.title')}
          onClose={() => setOpen(false)}
          footer={
            waitlisted !== null ? (
              <button className="btn block" onClick={() => setOpen(false)}>{t('common.close')}</button>
            ) : step === 'details' ? (
              <>
                <button className="btn ghost" onClick={() => setOpen(false)}>{t('common.cancel')}</button>
                <button className="btn" disabled={!name.trim() || !phone.trim()} onClick={() => setStep('review')}>
                  {t('booking.continue')}
                </button>
              </>
            ) : (
              <>
                <button className="btn ghost" onClick={() => setStep('details')}>{t('booking.back')}</button>
                <button className="btn" disabled={submitting} onClick={isFullDay && !canBook ? addToWaitlist : submit}>
                  {submitting
                    ? t('booking.confirming')
                    : isFullDay && !canBook
                      ? t('booking.joinWaitlist')
                      : detail.bookingMode === 'Request'
                        ? t('booking.request')
                        : t('booking.confirm')}
                </button>
              </>
            )
          }
        >
          {waitlisted !== null ? (
            <div className="stack stack-3" style={{ textAlign: 'center' }}>
              <span style={{ fontSize: '2rem' }} aria-hidden>⏳</span>
              <h3>{t('booking.waitlistJoined', { position: waitlisted })}</h3>
            </div>
          ) : (
            <>
              <div className="steps" style={{ marginBottom: 'var(--sp-5)' }}>
                <span className={`step ${step === 'details' ? 'current' : 'done'}`}>
                  <span className="dot">1</span> {t('booking.yourDetails')}
                </span>
                <span className="step-sep" />
                <span className={`step ${step === 'review' ? 'current' : ''}`}>
                  <span className="dot">2</span> {t('booking.review')}
                </span>
              </div>

              {step === 'details' && (
                <div className="stack stack-4">
                  <label className="field">
                    <span>{t('booking.name')}</span>
                    <input value={name} onChange={(e) => setName(e.target.value)} autoComplete="name" required />
                  </label>
                  <label className="field">
                    <span>{t('booking.phone')}</span>
                    <input value={phone} onChange={(e) => setPhone(e.target.value)} inputMode="tel" autoComplete="tel" required />
                  </label>
                  <label className="field">
                    <span>{t('booking.email')}</span>
                    <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} autoComplete="email" />
                  </label>
                  <label className="field">
                    <span>{t('booking.requestsOptional')}</span>
                    <textarea value={requests} onChange={(e) => setRequests(e.target.value)} maxLength={500} />
                    <span className="hint">{t('booking.requestsHint')}</span>
                  </label>
                </div>
              )}

              {step === 'review' && (
                <div className="stack stack-4">
                  <div className="card card-pad stack stack-2">
                    <strong>{i18n.language === 'ar' ? detail.nameAr : detail.name}</strong>
                    <span className="small muted">
                      {i18n.language === 'ar' ? detail.restaurant.nameAr : detail.restaurant.name} ·{' '}
                      {i18n.language === 'ar' ? detail.restaurant.areaNameAr : detail.restaurant.areaName}
                    </span>
                    <div className="divider" style={{ margin: 'var(--sp-2) 0' }} />
                    <div className="price-row"><span className="soft">{t('booking.date')}</span><span>{date}</span></div>
                    {selectedSlot && (
                      <div className="price-row">
                        <span className="soft">{t('booking.time')}</span>
                        <span>{formatTime(selectedSlot.startTime, i18n.language)} – {formatTime(selectedSlot.endTime, i18n.language)}</span>
                      </div>
                    )}
                    <div className="price-row">
                      <span className="soft">{t('booking.guests')}</span>
                      <span>{t('booking.guestsLine', { adults, children })}</span>
                    </div>
                    <div className="price-row total">
                      <span>{t('price.total')}</span>
                      <span className="nums">{money(quote.total, detail.currencyCode, i18n.language)}</span>
                    </div>
                  </div>

                  <div className="alert info">
                    {detail.bookingMode === 'Request' ? t('bookingMode.requestHint') : t('bookingMode.instantHint')}
                    {' · '}
                    {t('detail.cancellation', { minutes: detail.cancellationCutoffMinutes })}
                  </div>

                  {error && <div className="alert bad">{error}</div>}
                </div>
              )}
            </>
          )}
        </Sheet>
      )}
    </>
  )
}
