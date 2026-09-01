import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { Route, Routes } from 'react-router-dom'
import { Header } from './components/Header'
import { ProtectedRoute } from './components/ProtectedRoute'
import { AdminDashboard } from './pages/AdminDashboard'
import { AdminLogin } from './pages/AdminLogin'
import { CustomerHome } from './pages/CustomerHome'
import { RestaurantDashboard } from './pages/RestaurantDashboard'
import { RestaurantDetail } from './pages/RestaurantDetail'
import { RestaurantLogin } from './pages/RestaurantLogin'
import { RestaurantSignup } from './pages/RestaurantSignup'

function App() {
  const { i18n } = useTranslation()

  useEffect(() => {
    document.documentElement.dir = i18n.language === 'ar' ? 'rtl' : 'ltr'
    document.documentElement.lang = i18n.language
  }, [i18n.language])

  return (
    <>
      <Header />
      <main>
        <Routes>
          <Route path="/" element={<CustomerHome />} />
          <Route path="/restaurants/:id" element={<RestaurantDetail />} />
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
        </Routes>
      </main>
    </>
  )
}

export default App
