import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { NavLink, Route, Routes } from 'react-router-dom'
import { NotFound } from './NotFound'
import { api } from '../api/client'
import type { RestaurantProfile } from '../types'
import { BookingsPage } from './dashboard/BookingsPage'
import { CalendarPage } from './dashboard/CalendarPage'
import { OverviewPage } from './dashboard/OverviewPage'
import { ProfileEdit } from './dashboard/ProfileEdit'
import { ServiceEditorPage } from './dashboard/ServiceEditorPage'
import { ServicesPage } from './dashboard/ServicesPage'
import { BookingSettingsPage } from './dashboard/BookingSettingsPage'

export function RestaurantDashboard() {
  const { t } = useTranslation()
  const [profile, setProfile] = useState<RestaurantProfile | null>(null)

  useEffect(() => {
    api.get<RestaurantProfile>('/dashboard/profile').then((r) => setProfile(r.data)).catch(() => setProfile(null))
  }, [])

  return (
    <div className="container section-tight">
      {profile?.status === 'Pending' && <div className="alert warn" style={{ marginBottom: 'var(--sp-4)' }}>{t('dashboard.pendingBanner')}</div>}
      {profile?.status === 'Suspended' && <div className="alert bad" style={{ marginBottom: 'var(--sp-4)' }}>{t('dashboard.suspendedBanner')}</div>}

      <div className="dash-layout">
        <nav className="dash-nav" aria-label={t('dashboard.title')}>
          <NavLink to="/dashboard" end className={({ isActive }) => (isActive ? 'active' : '')}>{t('dashboard.overview')}</NavLink>
          <NavLink to="/dashboard/services" className={({ isActive }) => (isActive ? 'active' : '')}>{t('dashboard.services')}</NavLink>
          <NavLink to="/dashboard/bookings" className={({ isActive }) => (isActive ? 'active' : '')}>{t('dashboard.bookings')}</NavLink>
          <NavLink to="/dashboard/calendar" className={({ isActive }) => (isActive ? 'active' : '')}>{t('dashboard.calendar')}</NavLink>
          <NavLink to="/dashboard/profile" className={({ isActive }) => (isActive ? 'active' : '')}>{t('dashboard.profile')}</NavLink>
          <NavLink to="/dashboard/settings" className={({ isActive }) => (isActive ? 'active' : '')}>{t('dashboard.settings')}</NavLink>
        </nav>

        <div style={{ minWidth: 0 }}>
          <Routes>
            <Route index element={<OverviewPage />} />
            <Route path="services" element={<ServicesPage />} />
            <Route path="services/new" element={<ServiceEditorPage />} />
            <Route path="services/:id" element={<ServiceEditorPage />} />
            <Route path="bookings" element={<BookingsPage />} />
            <Route path="calendar" element={<CalendarPage />} />
            <Route path="profile" element={<ProfileEdit />} />
            <Route path="settings" element={<BookingSettingsPage />} />
            <Route path="*" element={<NotFound />} />
          </Routes>
        </div>
      </div>
    </div>
  )
}
