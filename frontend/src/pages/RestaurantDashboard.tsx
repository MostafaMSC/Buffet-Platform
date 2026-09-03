import { Route, Routes } from 'react-router-dom'
import { BookingDashboardPage } from './dashboard/BookingDashboardPage'
import { BookingSettingsPage } from './dashboard/BookingSettingsPage'
import { BookingSetupPage } from './dashboard/BookingSetupPage'
import { DashboardHome } from './dashboard/DashboardHome'
import { OfferingFormPage } from './dashboard/OfferingFormPage'
import { ProfileEdit } from './dashboard/ProfileEdit'

export function RestaurantDashboard() {
  return (
    <Routes>
      <Route index element={<DashboardHome />} />
      <Route path="profile" element={<ProfileEdit />} />
      <Route path="offerings/new" element={<OfferingFormPage />} />
      <Route path="offerings/:id/edit" element={<OfferingFormPage />} />
      <Route path="offerings/:id/booking" element={<BookingSetupPage />} />
      <Route path="booking-settings" element={<BookingSettingsPage />} />
      <Route path="bookings" element={<BookingDashboardPage />} />
    </Routes>
  )
}
