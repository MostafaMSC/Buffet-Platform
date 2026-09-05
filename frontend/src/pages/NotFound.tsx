import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { EmptyState } from '../components/ui'

/// A mistyped or retired URL lands here rather than on a blank page between the header
/// and the footer.
export function NotFound() {
  const { t } = useTranslation()

  return (
    <div className="container section">
      <EmptyState
        icon="🧭"
        title={t('notFound.title')}
        message={t('notFound.text')}
        actions={
          <>
            <Link className="btn" to="/">{t('notFound.home')}</Link>
            <Link className="btn secondary" to="/search">{t('nav.explore')}</Link>
          </>
        }
      />
    </div>
  )
}
