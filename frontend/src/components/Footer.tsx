import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'

export function Footer() {
  const { t } = useTranslation()

  return (
    <footer className="site-footer">
      <div className="container">
        <div className="footer-grid">
          <div>
            <h4>{t('footer.discover')}</h4>
            <Link to="/search?type=Buffet">{t('serviceType.Buffet')}</Link>
            <Link to="/search?type=SetMenu">{t('serviceType.SetMenu')}</Link>
            <Link to="/search?availability=Today">{t('footer.availableToday')}</Link>
          </div>

          <div>
            <h4>{t('footer.cities')}</h4>
            <Link to="/search?city=baghdad">{t('city.baghdad')}</Link>
            <Link to="/search?city=erbil">{t('city.erbil')}</Link>
            <Link to="/search?city=basra">{t('city.basra')}</Link>
          </div>

          <div>
            <h4>{t('footer.bookings')}</h4>
            <Link to="/my-bookings">{t('nav.myBookings')}</Link>
            <Link to="/favorites">{t('nav.favorites')}</Link>
          </div>

          <div>
            <h4>{t('footer.forRestaurants')}</h4>
            <Link to="/restaurant/signup">{t('footer.listYourRestaurant')}</Link>
            <Link to="/restaurant/login">{t('nav.restaurantLogin')}</Link>
            <Link to="/admin/login">{t('nav.adminLogin')}</Link>
          </div>
        </div>

        <div className="divider" />
        <p className="tiny muted">© {new Date().getFullYear()} {t('appName')} · {t('footer.tagline')}</p>
      </div>
    </footer>
  )
}
