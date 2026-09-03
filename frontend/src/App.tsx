import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { Route, Routes, useLocation } from 'react-router-dom'
import { Footer } from './components/Footer'
import { Header } from './components/Header'
import { ProtectedRoute } from './components/ProtectedRoute'
import { AdminDashboard } from './pages/AdminDashboard'
import { AdminLogin } from './pages/AdminLogin'
import { BookingDetailPage } from './pages/BookingDetailPage'
import { Favorites } from './pages/Favorites'
import { Home } from './pages/Home'
import { MyBookings } from './pages/MyBookings'
import { NotFound } from './pages/NotFound'
import { RestaurantDashboard } from './pages/RestaurantDashboard'
import { RestaurantDetail } from './pages/RestaurantDetail'
import { RestaurantLogin } from './pages/RestaurantLogin'
import { RestaurantSignup } from './pages/RestaurantSignup'
import { Search } from './pages/Search'
import { ServiceDetail } from './pages/ServiceDetail'

function App() {
  const { i18n } = useTranslation()
  const location = useLocation()

  useEffect(() => {
    document.documentElement.dir = i18n.language === 'ar' ? 'rtl' : 'ltr'
    document.documentElement.lang = i18n.language
  }, [i18n.language])

  // A new page should start at the top, not wherever the previous one was scrolled to.
  useEffect(() => {
    window.scrollTo({ top: 0, behavior: 'instant' as ScrollBehavior })
  }, [location.pathname])

  return (
    <>
      <Header />
      <main style={{ flex: 1 }}>
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/search" element={<Search />} />
          <Route path="/services/:id" element={<ServiceDetail />} />
          <Route path="/restaurants/:id" element={<RestaurantDetail />} />
          <Route path="/bookings/:code" element={<BookingDetailPage />} />
          <Route path="/my-bookings" element={<MyBookings />} />
          <Route path="/favorites" element={<Favorites />} />

          <Route path="/restaurant/login" element={<RestaurantLogin />} />
          <Route path="/restaurant/signup" element={<RestaurantSignup />} />
          <Route
            path="/dashboard/*"
            element={
              <ProtectedRoute role="RestaurantOwner">
                <RestaurantDashboard />
              </ProtectedRoute>
            }
          />

          <Route path="/admin/login" element={<AdminLogin />} />
          <Route
            path="/admin"
            element={
              <ProtectedRoute role="Admin">
                <AdminDashboard />
              </ProtectedRoute>
            }
          />

          <Route path="*" element={<NotFound />} />
        </Routes>
      </main>
      <Footer />
    </>
  )
}

export default App
