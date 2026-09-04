import { useEffect, useRef, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'

/* ---------------------------------------------------------------- brand */

/// The header/footer mark: a tiered stand, the spread motif at the centre of the Maftooh
/// emblem, simplified to a glyph that stays crisp at 20px. Renders in `currentColor`, so
/// its gold-on-green comes from .brand-mark rather than being baked into the SVG.
export function Logo({ size = 19 }: { size?: number }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none" aria-hidden>
      <circle cx="12" cy="6.4" r="1.5" fill="currentColor" />
      <ellipse cx="12" cy="10.2" rx="3.3" ry="1.05" fill="currentColor" />
      <ellipse cx="12" cy="14.4" rx="5.6" ry="1.35" fill="currentColor" />
      <ellipse cx="12" cy="19" rx="8.2" ry="1.65" fill="currentColor" />
      <path d="M12 9.2v9.6" stroke="currentColor" strokeWidth="1.1" strokeLinecap="round" />
    </svg>
  )
}

/* ---------------------------------------------------------------- rating */

export function Stars({ rating, size = 14 }: { rating: number; size?: number }) {
  return (
    <span className="row" style={{ gap: 1 }} aria-hidden>
      {[0, 1, 2, 3, 4].map((i) => (
        <svg key={i} width={size} height={size} viewBox="0 0 20 20" fill={i < Math.round(rating) ? 'currentColor' : 'none'} stroke="currentColor" strokeWidth="1.5">
          <path d="M10 2.5l2.35 4.76 5.25.76-3.8 3.7.9 5.23L10 14.48l-4.7 2.47.9-5.23-3.8-3.7 5.25-.76z" strokeLinejoin="round" />
        </svg>
      ))}
    </span>
  )
}

/// A rating only renders when it is backed by reviews — a restaurant with none shows
/// nothing rather than a zero that reads as a bad score.
export function RatingInline({ rating, reviewCount }: { rating: number | null; reviewCount: number }) {
  const { t } = useTranslation()
  if (rating === null || reviewCount === 0) {
    return <span className="tiny muted">{t('rating.none')}</span>
  }
  return (
    <span className="rating-inline" title={t('rating.reviews', { count: reviewCount })}>
      <svg width="13" height="13" viewBox="0 0 20 20" fill="currentColor" aria-hidden>
        <path d="M10 2.5l2.35 4.76 5.25.76-3.8 3.7.9 5.23L10 14.48l-4.7 2.47.9-5.23-3.8-3.7 5.25-.76z" />
      </svg>
      <span className="nums">{rating.toFixed(1)}</span>
      <span className="muted" style={{ fontWeight: 400 }}>({reviewCount})</span>
    </span>
  )
}

/* ---------------------------------------------------------------- icons */

export function HeartIcon({ filled }: { filled: boolean }) {
  return (
    <svg width="17" height="17" viewBox="0 0 24 24" fill={filled ? 'currentColor' : 'none'} stroke="currentColor" strokeWidth="2" aria-hidden>
      <path d="M20.8 4.6a5.5 5.5 0 0 0-7.8 0L12 5.7l-1-1.1a5.5 5.5 0 0 0-7.8 7.8l1.1 1L12 21l7.7-7.6 1.1-1a5.5 5.5 0 0 0 0-7.8z" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  )
}

export function Icon({ name, size = 18 }: { name: 'search' | 'filter' | 'close' | 'chevron' | 'map' | 'calendar' | 'users' | 'check' | 'clock' | 'globe'; size?: number }) {
  const paths: Record<string, ReactNode> = {
    search: <><circle cx="11" cy="11" r="7" /><path d="M20 20l-3.5-3.5" strokeLinecap="round" /></>,
    filter: <><path d="M4 6h16M7 12h10M10 18h4" strokeLinecap="round" /></>,
    close: <path d="M6 6l12 12M18 6L6 18" strokeLinecap="round" />,
    chevron: <path d="M9 6l6 6-6 6" strokeLinecap="round" strokeLinejoin="round" />,
    map: <><path d="M9 3L3 6v15l6-3 6 3 6-3V3l-6 3-6-3z" strokeLinejoin="round" /><path d="M9 3v15M15 6v15" /></>,
    calendar: <><rect x="3" y="5" width="18" height="16" rx="2" /><path d="M3 10h18M8 3v4M16 3v4" strokeLinecap="round" /></>,
    users: <><circle cx="9" cy="8" r="3.5" /><path d="M2.5 20a6.5 6.5 0 0 1 13 0M17 11.5a3 3 0 1 0-1.5-5.6M18 20a5.8 5.8 0 0 0-2-4.2" strokeLinecap="round" /></>,
    check: <path d="M5 12.5l4.5 4.5L19 7" strokeLinecap="round" strokeLinejoin="round" />,
    globe: <><circle cx="12" cy="12" r="9" /><path d="M3 12h18M12 3c2.5 2.6 3.8 5.7 3.8 9S14.5 18.4 12 21c-2.5-2.6-3.8-5.7-3.8-9S9.5 5.6 12 3z" /></>,
    clock: <><circle cx="12" cy="12" r="9" /><path d="M12 7v5.5l3.5 2" strokeLinecap="round" /></>,
  }
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" aria-hidden>
      {paths[name]}
    </svg>
  )
}

/* ---------------------------------------------------------------- sheet */

/// One component for both a mobile bottom sheet and a desktop dialog — the CSS decides
/// which it looks like, so behaviour (focus, escape, backdrop) can't diverge between them.
export function Sheet({
  title,
  onClose,
  children,
  footer,
}: {
  title: string
  onClose: () => void
  children: ReactNode
  footer?: ReactNode
}) {
  const ref = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => { if (e.key === 'Escape') onClose() }
    document.addEventListener('keydown', onKey)
    const previous = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    ref.current?.focus()
    return () => {
      document.removeEventListener('keydown', onKey)
      document.body.style.overflow = previous
    }
  }, [onClose])

  return (
    <div className="sheet-backdrop" onClick={onClose} role="presentation">
      <div
        className="sheet"
        role="dialog"
        aria-modal="true"
        aria-label={title}
        tabIndex={-1}
        ref={ref}
        onClick={(e) => e.stopPropagation()}
      >
        <div className="sheet-head">
          <h3>{title}</h3>
          <button className="btn ghost sm square" onClick={onClose} aria-label="Close">
            <Icon name="close" size={18} />
          </button>
        </div>
        <div className="sheet-body">{children}</div>
        {footer && <div className="sheet-foot">{footer}</div>}
      </div>
    </div>
  )
}

/* ---------------------------------------------------------------- states */

export function Skeleton({ height = 16, width = '100%', radius }: { height?: number | string; width?: number | string; radius?: number }) {
  return <div className="skeleton" style={{ height, width, borderRadius: radius }} />
}

export function CardSkeleton() {
  return (
    <div className="service-card">
      <Skeleton height="0" width="100%" />
      <div className="skeleton" style={{ aspectRatio: '4 / 3', borderRadius: 'var(--r-md)' }} />
      <div className="stack stack-2">
        <Skeleton height={13} width="65%" />
        <Skeleton height={13} width="45%" />
        <Skeleton height={13} width="35%" />
      </div>
    </div>
  )
}

export function EmptyState({
  icon = '🍽️',
  title,
  message,
  actions,
}: {
  icon?: string
  title: string
  message?: string
  actions?: ReactNode
}) {
  return (
    <div className="empty-state">
      <div className="icon" aria-hidden>{icon}</div>
      <h3>{title}</h3>
      {message && <p>{message}</p>}
      {actions && <div className="actions">{actions}</div>}
    </div>
  )
}

/* ---------------------------------------------------------------- stepper */

export function Stepper({
  value,
  onChange,
  min = 0,
  max = 99,
  label,
}: {
  value: number
  onChange: (next: number) => void
  min?: number
  max?: number
  label: string
}) {
  return (
    <div className="stepper">
      <button type="button" onClick={() => onChange(Math.max(min, value - 1))} disabled={value <= min} aria-label={`${label} −`}>
        −
      </button>
      <span className="value" aria-live="polite">{value}</span>
      <button type="button" onClick={() => onChange(Math.min(max, value + 1))} disabled={value >= max} aria-label={`${label} +`}>
        +
      </button>
    </div>
  )
}

/* ---------------------------------------------------------------- misc */

export function Badge({ kind, children }: { kind?: 'buffet' | 'setmenu' | 'good' | 'warn' | 'bad' | 'solid'; children: ReactNode }) {
  return <span className={`badge ${kind ?? ''}`}>{children}</span>
}

export function ServiceTypeBadge({ type }: { type: 'Buffet' | 'SetMenu' }) {
  const { t } = useTranslation()
  return <Badge kind={type === 'Buffet' ? 'buffet' : 'setmenu'}>{t(`serviceType.${type}`)}</Badge>
}

/// Availability stated plainly: a card that can't be booked says so, and a nearly-full one
/// says how little is left rather than implying plenty.
export function AvailabilityPill({
  isAvailable,
  spotsLeft,
  bookingEnabled,
}: {
  isAvailable: boolean
  spotsLeft: number | null
  bookingEnabled: boolean
}) {
  const { t } = useTranslation()

  if (!bookingEnabled) return <span className="tiny muted">{t('availability.walkIn')}</span>
  if (!isAvailable) return <Badge kind="bad">{t('availability.notAvailable')}</Badge>
  if (spotsLeft !== null && spotsLeft <= 10) return <Badge kind="warn">{t('availability.spotsLeft', { count: spotsLeft })}</Badge>
  return <Badge kind="good">{t('availability.available')}</Badge>
}
