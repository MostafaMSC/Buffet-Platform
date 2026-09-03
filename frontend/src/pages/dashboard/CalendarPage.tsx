import { useCallback, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { getCalendar, getDashboardServices, setSlotOverride } from '../../api/endpoints'
import { EmptyState, Sheet, Skeleton } from '../../components/ui'
import type { CalendarDay, DashboardService } from '../../types'
import { apiError, formatDate, formatTime, todayInBaghdad } from '../../utils/format'

/// A month at a glance: how full each day is, and a tap into any day to close a sitting or
/// resize it for that date only.
export function CalendarPage() {
  const { t, i18n } = useTranslation()
  const today = todayInBaghdad()

  const [monthStart, setMonthStart] = useState(() => `${today.slice(0, 7)}-01`)
  const [serviceId, setServiceId] = useState<number | ''>('')
  const [services, setServices] = useState<DashboardService[]>([])
  const [days, setDays] = useState<CalendarDay[] | null>(null)
  const [selected, setSelected] = useState<CalendarDay | null>(null)
  const [error, setError] = useState<string | null>(null)

  const monthEnd = (() => {
    const d = new Date(`${monthStart}T00:00:00`)
    d.setMonth(d.getMonth() + 1)
    d.setDate(0)
    return d.toISOString().slice(0, 10)
  })()

  useEffect(() => { getDashboardServices(1).then(setServices).catch(() => setServices([])) }, [])

  const load = useCallback(() => {
    setDays(null)
    getCalendar(monthStart, monthEnd, serviceId === '' ? undefined : serviceId)
      .then(setDays)
      .catch(() => setDays([]))
  }, [monthStart, monthEnd, serviceId])

  useEffect(load, [load])

  const shiftMonth = (delta: number) => {
    const d = new Date(`${monthStart}T00:00:00`)
    d.setMonth(d.getMonth() + delta)
    setMonthStart(d.toISOString().slice(0, 8) + '01')
  }

  const override = async (timeSlotId: number, date: string, payload: { isBlocked: boolean; capacity: number | null; note: string | null }) => {
    setError(null)
    try {
      await setSlotOverride({ timeSlotId, date, ...payload })
      load()
      setSelected(null)
    } catch (err) {
      setError(apiError(err, t('common.error')))
    }
  }

  // Pad the grid so the 1st lands under the right weekday column.
  const leadingBlanks = days && days.length > 0 ? new Date(`${days[0].date}T00:00:00`).getDay() : 0

  return (
    <div className="stack stack-5">
      <div className="section-head">
        <div>
          <h1 style={{ fontSize: '1.5rem' }}>{t('calendar.title')}</h1>
          <p>{t('calendar.subtitle')}</p>
        </div>
      </div>

      <div className="row-between wrap" style={{ gap: 'var(--sp-3)' }}>
        <div className="row" style={{ gap: 'var(--sp-2)' }}>
          <button className="btn secondary sm" onClick={() => shiftMonth(-1)}>← {t('calendar.prev')}</button>
          <strong>{formatDate(monthStart, i18n.language, { month: 'long', year: 'numeric' })}</strong>
          <button className="btn secondary sm" onClick={() => shiftMonth(1)}>{t('calendar.next')} →</button>
        </div>

        <label className="field" style={{ width: 220 }}>
          <span className="sr-only">{t('calendar.allServices')}</span>
          <select value={serviceId} onChange={(e) => setServiceId(e.target.value === '' ? '' : Number(e.target.value))}>
            <option value="">{t('calendar.allServices')}</option>
            {services.map((s) => (
              <option key={s.id} value={s.id}>{i18n.language === 'ar' ? s.nameAr : s.name}</option>
            ))}
          </select>
        </label>
      </div>

      {error && <div className="alert bad">{error}</div>}

      {!days && <Skeleton height={360} radius={14} />}

      {days?.length === 0 && <EmptyState icon="📅" title={t('services.empty')} message={t('services.emptyText')} />}

      {days && days.length > 0 && (
        <>
          <div className="calendar-grid">
            {['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'].map((day) => (
              <div key={day} className="tiny muted" style={{ textAlign: 'center', fontWeight: 700 }}>
                {t(`weekday.${day}`)}
              </div>
            ))}

            {Array.from({ length: leadingBlanks }, (_, i) => <div key={`blank-${i}`} className="calendar-day empty" />)}

            {days.map((day) => {
              const fill = day.totalCapacity > 0 ? Math.round((day.totalBooked / day.totalCapacity) * 100) : 0
              return (
                <button
                  key={day.date}
                  className={`calendar-day ${day.date === today ? 'today' : ''} ${day.services.length === 0 ? 'empty' : ''}`}
                  onClick={() => day.services.length > 0 && setSelected(day)}
                >
                  <span className="num">{Number(day.date.slice(-2))}</span>
                  {day.services.length > 0 ? (
                    <>
                      <span className="fill nums">{day.totalBooked}/{day.totalCapacity}</span>
                      <div className={`meter ${fill >= 100 ? 'full' : fill >= 80 ? 'high' : ''}`}>
                        <span style={{ width: `${Math.min(100, fill)}%` }} />
                      </div>
                    </>
                  ) : (
                    <span className="fill">—</span>
                  )}
                </button>
              )
            })}
          </div>

          <p className="tiny muted">{t('calendar.subtitle')}</p>
        </>
      )}

      {selected && (
        <Sheet title={formatDate(selected.date, i18n.language, { weekday: 'long', day: 'numeric', month: 'long' })} onClose={() => setSelected(null)}>
          {selected.services.length === 0 && <p className="small muted">{t('calendar.nothingOn')}</p>}

          <div className="stack stack-5">
            {selected.services.map((service) => (
              <div key={service.serviceId} className="stack stack-3">
                <strong>{i18n.language === 'ar' ? service.serviceNameAr : service.serviceName}</strong>

                {service.slots.map((slot) => (
                  <SlotRow
                    key={slot.timeSlotId ?? 'window'}
                    date={selected.date}
                    slot={slot}
                    onSave={override}
                  />
                ))}
              </div>
            ))}
          </div>
        </Sheet>
      )}
    </div>
  )
}

function SlotRow({
  date,
  slot,
  onSave,
}: {
  date: string
  slot: { timeSlotId: number | null; startTime: string; endTime: string; capacity: number; booked: number; isBlocked: boolean; note: string | null }
  onSave: (timeSlotId: number, date: string, payload: { isBlocked: boolean; capacity: number | null; note: string | null }) => void
}) {
  const { t, i18n } = useTranslation()
  const [capacity, setCapacity] = useState(String(slot.capacity))
  const [note, setNote] = useState(slot.note ?? '')

  // A whole-window service has no slot row to override; its capacity is edited on the
  // service itself.
  if (slot.timeSlotId === null) {
    return (
      <div className="card card-pad-sm row-between">
        <span className="small">{formatTime(slot.startTime, i18n.language)}–{formatTime(slot.endTime, i18n.language)}</span>
        <span className="small nums">{t('calendar.seatsBooked', { booked: slot.booked, capacity: slot.capacity })}</span>
      </div>
    )
  }

  return (
    <div className="card card-pad-sm stack stack-3">
      <div className="row-between wrap">
        <span className="strong small">{formatTime(slot.startTime, i18n.language)}–{formatTime(slot.endTime, i18n.language)}</span>
        <span className="small nums muted">{t('calendar.seatsBooked', { booked: slot.booked, capacity: slot.capacity })}</span>
      </div>

      <div className="row wrap" style={{ gap: 'var(--sp-2)', alignItems: 'flex-end' }}>
        <label className="field" style={{ width: 130 }}>
          <span>{t('calendar.overrideCapacity')}</span>
          <input type="number" min={0} value={capacity} onChange={(e) => setCapacity(e.target.value)} />
        </label>
        <label className="field grow" style={{ minWidth: 150 }}>
          <span>{t('calendar.note')}</span>
          <input value={note} onChange={(e) => setNote(e.target.value)} />
        </label>
      </div>

      <div className="row" style={{ gap: 'var(--sp-2)' }}>
        <button
          className="btn sm"
          onClick={() => onSave(slot.timeSlotId!, date, { isBlocked: false, capacity: capacity === '' ? null : Number(capacity), note: note || null })}
        >
          {t('calendar.saveDay')}
        </button>
        {slot.isBlocked ? (
          <button className="btn secondary sm" onClick={() => onSave(slot.timeSlotId!, date, { isBlocked: false, capacity: null, note: null })}>
            {t('calendar.unblock')}
          </button>
        ) : (
          <button className="btn ghost sm" onClick={() => onSave(slot.timeSlotId!, date, { isBlocked: true, capacity: null, note: note || null })}>
            {t('calendar.block')}
          </button>
        )}
      </div>
    </div>
  )
}
