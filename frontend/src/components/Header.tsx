import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

export function Header() {
  const { t, i18n } = useTranslation()
  const { token, role, logout } = useAuth()
  const navigate = useNavigate()

  const toggleLanguage = () => {
    const next = i18n.language === 'ar' ? 'en' : 'ar'
    i18n.changeLanguage(next)
  }

  const handleLogout = () => {
    logout()
    navigate('/')
  }

  return (
    <header className="site-header">
      <div className="container">
        <Link to="/" className="brand">
          <span className="brand-mark">🍽</span>
          <span>{t('appName')}</span>
        </Link>

        <div className="header-actions">
          {!token && (
            <>
              <Link className="nav-link" to="/restaurant/login">
                {t('nav.restaurantLogin')}
              </Link>
            </>
          )}
          {token && role === 'RestaurantOwner' && (
            <>
              <Link className="nav-link primary" to="/dashboard">
                {t('nav.dashboard')}
              </Link>
              <button className="nav-link" onClick={handleLogout}>
                {t('nav.logout')}
              </button>
            </>
          )}
          {token && role === 'Admin' && (
            <>
              <Link className="nav-link primary" to="/admin">
                {t('nav.dashboard')}
              </Link>
              <button className="nav-link" onClick={handleLogout}>
                {t('nav.logout')}
              </button>
            </>
          )}
          <button className="lang-toggle" onClick={toggleLanguage}>
            {i18n.language === 'ar' ? 'EN' : 'ع'}
          </button>
        </div>
      </div>
    </header>
  )
}
