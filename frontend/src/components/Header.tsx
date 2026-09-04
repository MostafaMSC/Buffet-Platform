import { useTranslation } from 'react-i18next'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { useFavorites } from '../hooks/useFavorites'
import { HeartIcon, Icon, Logo } from './ui'

export function Header() {
  const { t, i18n } = useTranslation()
  const { token, role, logout } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const { count } = useFavorites()

  const toggleLanguage = () => i18n.changeLanguage(i18n.language === 'ar' ? 'en' : 'ar')
  const handleLogout = () => { logout(); navigate('/') }
  const isActive = (path: string) => location.pathname === path

  return (
    <header className="site-header">
      <div className="container">
        <Link to="/" className="brand" aria-label={t('appName')}>
          <span className="brand-mark"><Logo /></span>
          <span>{t('appName')}</span>
        </Link>

        <nav className="nav" aria-label={t('nav.main')}>
          <Link className={`nav-link desktop-only ${isActive('/search') ? 'active' : ''}`} to="/search">
            <Icon name="search" size={16} />
            {t('nav.explore')}
          </Link>

          <Link className={`nav-link ${isActive('/favorites') ? 'active' : ''}`} to="/favorites" aria-label={t('nav.favorites')}>
            <HeartIcon filled={count > 0} />
            <span className="desktop-only">{t('nav.favorites')}</span>
            {count > 0 && <span className="badge" style={{ padding: '1px 6px' }}>{count}</span>}
          </Link>

          <Link className={`nav-link ${isActive('/my-bookings') ? 'active' : ''}`} to="/my-bookings">
            <Icon name="calendar" size={16} />
            <span className="desktop-only">{t('nav.myBookings')}</span>
          </Link>

          {!token && (
            <Link className="nav-link outlined desktop-only" to="/restaurant/login">
              {t('nav.forRestaurants')}
            </Link>
          )}

          {token && (
            <>
              <Link className="nav-link outlined" to={role === 'Admin' ? '/admin' : '/dashboard'}>
                {t('nav.dashboard')}
              </Link>
              <button className="nav-link desktop-only" onClick={handleLogout}>
                {t('nav.logout')}
              </button>
            </>
          )}

          {/* Labelled with the language you get, not the one you are in. */}
          <button className="nav-link" onClick={toggleLanguage} aria-label={t('nav.switchLanguage')}>
            <Icon name="globe" size={16} />
            <span className="desktop-only">{i18n.language === 'ar' ? 'English' : 'العربية'}</span>
          </button>
        </nav>
      </div>
    </header>
  )
}
