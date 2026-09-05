import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { confirmWaitlistOffer, lookupBookings } from '../api/endpoints'
import { Badge, EmptyState, Icon } from '../components/ui'
import type { BookingDetail, MyLookupResult } from '../types'
import { apiError, formatDate, formatTime, money, todayInBaghdad } from '../utils/format'

type Tab = 'upcoming' | 'past' | 'cancelled' | 'waitlist'

const CANCELLED: string[] = ['Cancelled', 'Rejected', 'NoShow']

/// Bookings are found by the phone number they were made with — there are no customer
/// accounts, so the number is the key. It is remembered on the device for next time.
export function MyBookings() {
  const { t, i18n } = useTranslation()
  const [phone, setPhone] = useState(() => localStorage.getItem('buffet_phone') ?? '')
  const [result, setResult] = useState<MyLookupResult | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [tab, setTab] = useState<Tab>('upcoming')
  const [busyId, setBusyId] = useState<number | null>(null)

  const today = todayInBaghdad()

  const search = async (e?: React.FormEvent) => {
    e?.preventDefault()
    setLoading(true)
    setError(null)
    try {
      const data = await lookupBookings(phone.trim())
      setResult(data)
      localStorage.setItem('buffet_phone', phone.trim())
    } catch (err) {
      setError(apiError(err, t('common.error'), t))
    } finally {
      setLoading(false)
    }
  }

  const confirmOffer = async (waitlistId: number) => {
    setBusyId(waitlistId)
    setError(null)
    try {
      await confirmWaitlistOffer(waitlistId, phone.trim())
      await search()
    } catch (err) {
      setError(apiError(err, t('common.error'), t))
    } finally {
      setBusyId(null)
    }
  }

  const bookings = result?.bookings ?? []
  const buckets: Record<Tab, BookingDetail[]> = {
    upcoming: bookings.filter((b) => b.date >= today && !CANCELLED.includes(b.status)),
    past: bookings.filter((b) => b.date < today && !CANCELLED.includes(b.status)),
    cancelled: bookings.filter((b) => CANCELLED.includes(b.status)),
    waitlist: [],
  }

  const waitlist = result?.waitlistEntries ?? []
  const counts: Record<Tab, number> = {
    upcoming: buckets.upcoming.length,
    past: buckets.past.length,
    cancelled: buckets.cancelled.length,
    waitlist: waitlist.length,
  }

  return (
    <div className="container container-narrow section">
      <div className="stack stack-2" style={{ marginBottom: 'var(--sp-5)' }}>
        <h1>{t('booking.lookupTitle')}</h1>
        <p className="soft">{t('booking.lookupSubtitle')}</p>
      </div>

      <form className="row wrap" onSubmit={search} style={{ gap: 'var(--sp-2)', marginBottom: 'var(--sp-5)' }}>
        <label className="field grow" style={{ minWidth: 220 }}>
          <span className="sr-only">{t('booking.phone')}</span>
          <input
            value={phone}
            onChange={(e) => setPhone(e.target.value)}
            placeholder="07XX XXX XXXX"
            inputMode="tel"
            autoComplete="tel"
            required
          />
        </label>
        <button className="btn" type="submit" disabled={loading}>
          <Icon name="search" size={16} />
          {t('booking.lookupAction')}
        </button>
      </form>

      {error && <div className="alert bad" style={{ marginBottom: 'var(--sp-4)' }}>{error}</div>}

      {result && (
        <>
          <div className="filter-bar" style={{ marginBottom: 'var(--sp-4)' }}>
            {(['upcoming', 'past', 'cancelled', 'waitlist'] as Tab[]).map((key) => (
              <button
                key={key}
                className={`chip ${tab === key ? 'active' : ''}`}
                onClick={() => setTab(key)}
              >
                {t(key === 'cancelled' ? 'booking.cancelledTab' : `booking.${key}`)}
                {counts[key] > 0 && <span className="badge">{counts[key]}</span>}
              </button>
            ))}
          </div>

          {tab !== 'waitlist' && buckets[tab].length === 0 && (
            <EmptyState
              icon="📭"
              title={t('booking.noBookings')}
              actions={<Link className="btn" to="/search">{t('nav.explore')}</Link>}
            />
          )}

          {tab !== 'waitlist' && (
            <div className="stack stack-3">
              {buckets[tab].map((booking) => (
                <Link key={booking.id} to={`/bookings/${booking.confirmationCode}`} className="card card-pad-sm row" style={{ gap: 'var(--sp-4)' }}>
                  {booking.photoUrl && (
                    <img src={booking.photoUrl} alt="" style={{ width: 68, height: 68, borderRadius: 'var(--r-sm)', objectFit: 'cover' }} />
                  )}
                  <div className="grow stack" style={{ gap: 2, minWidth: 0 }}>
                    <div className="row-between">
                      <strong className="truncate">
                        {i18n.language === 'ar' ? booking.restaurantNameAr : booking.restaurantName}
                      </strong>
                      <Badge kind={booking.status === 'Confirmed' ? 'good' : booking.status === 'Pending' ? 'warn' : 'bad'}>
                        {t(`bookingStatus.${booking.status}`)}
                      </Badge>
                    </div>
                    <span className="small soft truncate">
                      {i18n.language === 'ar' ? booking.serviceNameAr : booking.serviceName}
                    </span>
                    <span className="tiny muted">
                      {formatDate(booking.date, i18n.language)}
                      {booking.slotStartTime ? ` · ${formatTime(booking.slotStartTime, i18n.language)}` : ''}
                      {' · '}{t('search.guestCount', { count: booking.partySize })}
                      {' · '}{money(booking.totalPrice, booking.currencyCode, i18n.language)}
                    </span>
                  </div>
                  <span className="muted" aria-hidden><Icon name="chevron" size={16} /></span>
                </Link>
              ))}
            </div>
          )}

          {tab === 'waitlist' && (
            waitlist.length === 0 ? (
              <EmptyState icon="⏳" title={t('booking.noBookings')} />
            ) : (
              <div className="stack stack-3">
                {waitlist.map((entry) => (
                  <div key={entry.id} className="card card-pad-sm row-between wrap" style={{ gap: 'var(--sp-3)' }}>
                    <div className="stack" style={{ gap: 2 }}>
                      <strong>{i18n.language === 'ar' ? entry.restaurantNameAr : entry.restaurantName}</strong>
                      <span className="tiny muted">
                        {formatDate(entry.date, i18n.language)}
                        {entry.slotStartTime ? ` · ${formatTime(entry.slotStartTime, i18n.language)}` : ''}
                        {' · #'}{entry.position}
                      </span>
                    </div>
                    {entry.status === 'Offered' ? (
                      <button className="btn sm" disabled={busyId === entry.id} onClick={() => confirmOffer(entry.id)}>
                        {t('booking.confirmOffer')}
                      </button>
                    ) : (
                      <Badge>{t(`waitlistStatus.${entry.status}`)}</Badge>
                    )}
                  </div>
                ))}
              </div>
            )
          )}
        </>
      )}
    </div>
  )
}
