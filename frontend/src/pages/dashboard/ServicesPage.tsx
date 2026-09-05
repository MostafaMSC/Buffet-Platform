import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { deleteService, getDashboardServices, toggleAvailability } from '../../api/endpoints'
import { Badge, EmptyState, ServiceTypeBadge, Skeleton } from '../../components/ui'
import type { DashboardService } from '../../types'
import { formatDate, formatTime, money } from '../../utils/format'

export function ServicesPage() {
  const { t, i18n } = useTranslation()
  const [services, setServices] = useState<DashboardService[] | null>(null)

  const load = () => { getDashboardServices(7).then(setServices).catch(() => setServices([])) }
  useEffect(load, [])

  const remove = async (id: number) => {
    if (!confirm(t('services.deleteConfirm'))) return
    await deleteService(id)
    load()
  }

  const toggleDay = async (serviceId: number, date: string, isActive: boolean) => {
    // Optimistic: the switch is the whole point of the row, so it must feel instant.
    setServices((prev) => prev?.map((s) => s.id === serviceId
      ? { ...s, days: s.days.map((d) => (d.date === date ? { ...d, isActive: !isActive } : d)) }
      : s) ?? null)
    try {
      await toggleAvailability(serviceId, date, !isActive)
    } catch {
      load()
    }
  }

  return (
    <div className="stack stack-5">
      <div className="section-head">
        <div>
          <h1 style={{ fontSize: '1.5rem' }}>{t('services.title')}</h1>
          <p>{t('services.subtitle')}</p>
        </div>
        <Link className="btn" to="/dashboard/services/new">+ {t('services.add')}</Link>
      </div>

      {!services && <div className="stack stack-3">{Array.from({ length: 3 }, (_, i) => <Skeleton key={i} height={120} radius={14} />)}</div>}

      {services?.length === 0 && (
        <EmptyState
          icon="🍲"
          title={t('services.empty')}
          message={t('services.emptyText')}
          actions={<Link className="btn" to="/dashboard/services/new">{t('services.add')}</Link>}
        />
      )}

      {services?.map((service) => (
        <div className="card card-pad stack stack-4" key={service.id}>
          <div className="row-between wrap" style={{ gap: 'var(--sp-3)' }}>
            <div className="stack" style={{ gap: 4, minWidth: 0 }}>
              <div className="row wrap" style={{ gap: 'var(--sp-2)' }}>
                <ServiceTypeBadge type={service.serviceType} />
                <Badge kind={service.status === 'Active' ? 'good' : service.status === 'Paused' ? 'warn' : undefined}>
                  {t(`serviceStatus.${service.status}`)}
                </Badge>
                <Badge>{t(`mealType.${service.mealType}`)}</Badge>
              </div>
              <strong>{i18n.language === 'ar' ? service.nameAr : service.name}</strong>
              <span className="tiny muted">
                {formatTime(service.opensAt, i18n.language)}–{formatTime(service.closesAt, i18n.language)}
                {' · '}{t(`recurrence.${service.recurrence}`)}
                {' · '}
                {service.pricingModel === 'PerPackage'
                  ? t('price.perPackage', { amount: money(service.packagePrice ?? 0, 'IQD', i18n.language), guests: service.packageGuests ?? 2 })
                  : t('price.perPerson', { amount: money(service.pricePerAdult, 'IQD', i18n.language) })}
                {service.slotCount > 0 ? ` · ${service.slotCount} ${t('services.slots').toLowerCase()}` : service.capacity ? ` · ${service.capacity}` : ''}
              </span>
            </div>

            <div className="row" style={{ gap: 'var(--sp-2)' }}>
              <Link className="btn secondary sm" to={`/dashboard/services/${service.id}`}>{t('services.edit')}</Link>
              <button className="btn ghost sm" onClick={() => remove(service.id)}>{t('services.delete')}</button>
            </div>
          </div>

          <div className="row wrap" style={{ gap: 'var(--sp-2)' }}>
            {service.days.map((day) => (
              <button
                key={day.date}
                className={`chip sm ${day.isActive ? 'active' : ''}`}
                onClick={() => toggleDay(service.id, day.date, day.isActive)}
                title={day.date}
              >
                {formatDate(day.date, i18n.language, { weekday: 'short', day: 'numeric' })}
              </button>
            ))}
          </div>
        </div>
      ))}
    </div>
  )
}
