import {
  Children,
  isValidElement,
  useEffect,
  useId,
  useMemo,
  useRef,
  useState,
  type CSSProperties,
  type KeyboardEvent as ReactKeyboardEvent,
  type ReactNode,
} from 'react'
import { createPortal } from 'react-dom'
import { useTranslation } from 'react-i18next'

/* ---------------------------------------------------------------- select */

interface SelectOption { value: string; label: ReactNode; disabled?: boolean }
interface SelectGroup { label: string; options: SelectOption[] }
type SelectEntry = SelectOption | SelectGroup

function isGroup(entry: SelectEntry): entry is SelectGroup {
  return 'options' in entry
}

/// Reads a <Select>'s children exactly as if it were a native <select> — plain <option>s
/// and grouping <optgroup>s — so call sites never have to learn a different options shape.
function parseChildren(children: ReactNode): SelectEntry[] {
  const entries: SelectEntry[] = []
  Children.forEach(children, (child) => {
    if (!isValidElement(child)) return
    const props = child.props as { value?: string | number; children?: ReactNode; disabled?: boolean; label?: string }
    if (child.type === 'optgroup') {
      const options: SelectOption[] = []
      Children.forEach(props.children, (opt) => {
        if (!isValidElement(opt)) return
        const optProps = opt.props as { value?: string | number; children?: ReactNode; disabled?: boolean }
        options.push({ value: String(optProps.value ?? ''), label: optProps.children, disabled: optProps.disabled })
      })
      entries.push({ label: props.label ?? '', options })
    } else if (child.type === 'option') {
      entries.push({ value: String(props.value ?? ''), label: props.children, disabled: props.disabled })
    }
  })
  return entries
}

function flattenOptions(entries: SelectEntry[]): SelectOption[] {
  return entries.flatMap((entry) => (isGroup(entry) ? entry.options : [entry]))
}

/// A themeable stand-in for the native <select>: same value/onChange/<option> children
/// contract (so it drops into any existing form untouched), but the option list is our
/// own markup — portaled to <body> so it always escapes a scrolling ancestor like
/// .table-wrap instead of being clipped by it.
export function Select({
  value,
  onChange,
  children,
  className,
  disabled,
  style,
  'aria-label': ariaLabel,
  'aria-required': ariaRequired,
}: {
  value: string | number | undefined
  onChange: (e: { target: { value: string } }) => void
  children: ReactNode
  className?: string
  disabled?: boolean
  style?: CSSProperties
  'aria-label'?: string
  'aria-required'?: boolean
}) {
  const [open, setOpen] = useState(false)
  const [panelStyle, setPanelStyle] = useState<CSSProperties>({})
  const [activeValue, setActiveValue] = useState<string | null>(null)
  const triggerRef = useRef<HTMLButtonElement>(null)
  const panelRef = useRef<HTMLUListElement>(null)
  const listboxId = useId()

  const entries = useMemo(() => parseChildren(children), [children])
  const flat = useMemo(() => flattenOptions(entries), [entries])
  const currentValue = String(value ?? '')
  const selected = flat.find((o) => o.value === currentValue)

  const positionPanel = () => {
    const trigger = triggerRef.current
    if (!trigger) return
    const rect = trigger.getBoundingClientRect()
    const maxHeight = 280
    const spaceBelow = window.innerHeight - rect.bottom
    const openUp = spaceBelow < maxHeight && rect.top > spaceBelow
    // A chip-style trigger (the sort control) is only as wide as its own label, but the
    // panel still has to fit its longest option — so it's given a floor, not a fixed
    // width, and grows from whichever edge keeps it on screen.
    const isRtl = getComputedStyle(trigger).direction === 'rtl'
    setPanelStyle({
      position: 'fixed',
      minWidth: rect.width,
      maxWidth: window.innerWidth - 16,
      ...(isRtl ? { right: window.innerWidth - rect.right } : { left: rect.left }),
      ...(openUp
        ? { bottom: window.innerHeight - rect.top + 4, maxHeight: Math.min(maxHeight, rect.top - 8) }
        : { top: rect.bottom + 4, maxHeight: Math.min(maxHeight, spaceBelow - 8) }),
    })
  }

  const closePanel = () => setOpen(false)

  const openPanel = () => {
    if (disabled || flat.length === 0) return
    positionPanel()
    setActiveValue(flat.find((o) => o.value === currentValue && !o.disabled)?.value ?? flat.find((o) => !o.disabled)?.value ?? null)
    setOpen(true)
  }

  useEffect(() => {
    if (!open) return
    const onDocMouseDown = (e: MouseEvent) => {
      const target = e.target as Node
      if (triggerRef.current?.contains(target)) return
      if (panelRef.current?.contains(target)) return
      closePanel()
    }
    // A capture-phase window listener sees every scroll, including the panel's own list
    // scrolling past a long option set — that one must not close it.
    const onScroll = (e: Event) => {
      if (panelRef.current?.contains(e.target as Node)) return
      closePanel()
    }
    const onResize = () => closePanel()
    document.addEventListener('mousedown', onDocMouseDown)
    window.addEventListener('scroll', onScroll, true)
    window.addEventListener('resize', onResize)
    return () => {
      document.removeEventListener('mousedown', onDocMouseDown)
      window.removeEventListener('scroll', onScroll, true)
      window.removeEventListener('resize', onResize)
    }
  }, [open])

  useEffect(() => {
    if (!open) return
    panelRef.current?.querySelector('.select-option.selected')?.scrollIntoView({ block: 'nearest' })
  }, [open])

  const selectValue = (val: string) => {
    onChange({ target: { value: val } })
    closePanel()
    triggerRef.current?.focus()
  }

  const moveActive = (dir: 1 | -1) => {
    const enabled = flat.filter((o) => !o.disabled)
    if (enabled.length === 0) return
    const idx = enabled.findIndex((o) => o.value === activeValue)
    const next = enabled[(idx + dir + enabled.length) % enabled.length]
    setActiveValue(next.value)
  }

  const onTriggerKeyDown = (e: ReactKeyboardEvent<HTMLButtonElement>) => {
    if (disabled) return
    if (!open) {
      if (e.key === 'ArrowDown' || e.key === 'ArrowUp' || e.key === 'Enter' || e.key === ' ') {
        e.preventDefault()
        openPanel()
      }
      return
    }
    if (e.key === 'ArrowDown') { e.preventDefault(); moveActive(1) }
    else if (e.key === 'ArrowUp') { e.preventDefault(); moveActive(-1) }
    else if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); if (activeValue != null) selectValue(activeValue) }
    else if (e.key === 'Escape') { e.preventDefault(); closePanel() }
    else if (e.key === 'Tab') closePanel()
  }

  const optionId = (val: string) => `${listboxId}-${encodeURIComponent(val)}`

  const renderOption = (opt: SelectOption) => {
    // A disabled option (the AreaSelect placeholder, say) can be the current value before
    // a real choice is made, but it isn't a selection — don't dress it up as one.
    const isSelected = opt.value === currentValue && !opt.disabled
    const isActive = opt.value === activeValue
    return (
      <li
        key={opt.value}
        id={optionId(opt.value)}
        role="option"
        aria-selected={isSelected}
        aria-disabled={opt.disabled}
        className={`select-option${isSelected ? ' selected' : ''}${isActive ? ' active' : ''}${opt.disabled ? ' disabled' : ''}`}
        onMouseEnter={() => !opt.disabled && setActiveValue(opt.value)}
        onClick={() => !opt.disabled && selectValue(opt.value)}
      >
        <Icon name="check" size={14} />
        <span>{opt.label}</span>
      </li>
    )
  }

  return (
    <>
      <button
        type="button"
        ref={triggerRef}
        className={`select-trigger${className ? ` ${className}` : ''}`}
        style={style}
        disabled={disabled}
        aria-haspopup="listbox"
        aria-expanded={open}
        aria-required={ariaRequired}
        aria-label={ariaLabel}
        aria-activedescendant={open && activeValue != null ? optionId(activeValue) : undefined}
        onClick={() => (open ? closePanel() : openPanel())}
        onKeyDown={onTriggerKeyDown}
      >
        <span className="select-value">{selected ? selected.label : currentValue}</span>
        <span className="select-chevron"><Icon name="chevron" size={14} /></span>
      </button>
      {open &&
        createPortal(
          <ul className="select-panel" role="listbox" ref={panelRef} id={listboxId} aria-label={ariaLabel} style={panelStyle}>
            {entries.map((entry, i) =>
              isGroup(entry) ? (
                <li key={`group-${i}`} role="presentation" className="select-group">
                  <div className="select-group-label">{entry.label}</div>
                  <ul role="group" aria-label={entry.label} className="select-group-options">
                    {entry.options.map(renderOption)}
                  </ul>
                </li>
              ) : (
                renderOption(entry)
              ),
            )}
          </ul>,
          document.body,
        )}
    </>
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
  // Every call site passes an inline arrow for onClose, so the prop is a new function on
  // each render — including the render behind every keystroke in a field inside the sheet.
  // Reading it through a ref keeps the setup below a mount-only effect; depending on the
  // prop directly re-ran it per keystroke, and the focus() it takes on open then pulled the
  // caret out of the field being typed into after a single character.
  const closeRef = useRef(onClose)
  useEffect(() => { closeRef.current = onClose })

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => { if (e.key === 'Escape') closeRef.current() }
    document.addEventListener('keydown', onKey)
    const previous = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    ref.current?.focus()
    return () => {
      document.removeEventListener('keydown', onKey)
      document.body.style.overflow = previous
    }
  }, [])

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
