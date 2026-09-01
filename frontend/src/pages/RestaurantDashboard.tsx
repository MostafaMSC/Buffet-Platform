import { Route, Routes } from 'react-router-dom'
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
    </Routes>
  )
}
