import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router-dom'
import { getHomeFeed } from '../api/endpoints'
import { CategoryNav, type CategoryValue } from '../components/CategoryNav'
import { SearchBar, type SearchBarValue } from '../components/SearchBar'
import { ServiceCard } from '../components/ServiceCard'
import { CardSkeleton, EmptyState } from '../components/ui'
import type { HomeFeed, ServiceCard as ServiceCardType } from '../types'
import { todayInBaghdad } from '../utils/format'

export function Home() {
  const { t, i18n } = useTranslation()
  const navigate = useNavigate()
  const isAr = i18n.language === 'ar'

  const [category, setCategory] = useState<CategoryValue>('All')
  const [search, setSearch] = useState<SearchBarValue>({
    city: '',
    areaId: undefined,
    date: todayInBaghdad(),
    time: '',
    guests: 2,
  })
  const [feed, setFeed] = useState<HomeFeed | null>(null)
  const [failed, setFailed] = useState(false)

  useEffect(() => {
    getHomeFeed().then(setFeed).catch(() => setFailed(true))
  }, [])

  const runSearch = (overrides: Record<string, string> = {}) => {
    const params = new URLSearchParams()
    if (search.city) params.set('city', search.city)
    if (search.areaId) params.set('areaId', String(search.areaId))
    if (search.date) params.set('date', search.date)
    if (search.time) params.set('time', search.time)
    if (search.guests) params.set('guests', String(search.guests))
    if (category !== 'All') params.set('type', category)
    Object.entries(overrides).forEach(([k, v]) => params.set(k, v))
    navigate(`/search?${params}`)
  }

  return (
    <>
      <section className="container hero">
        <h1>{t('home.heroTitle')}</h1>
        <p>{t('home.heroSubtitle')}</p>
        <SearchBar value={search} onChange={setSearch} onSubmit={() => runSearch()} />
      </section>

      <div className="container">
        <CategoryNav
          value={category}
          onChange={(next) => {
            setCategory(next)
            runSearch(next === 'All' ? {} : { type: next })
          }}
        />
      </div>

      {failed && (
        <div className="container section">
          <EmptyState
            icon="⚠️"
            title={t('search.errorTitle')}
            message={t('search.errorText')}
            actions={<button className="btn secondary" onClick={() => location.reload()}>{t('common.retry')}</button>}
          />
        </div>
      )}

      <Row
        title={t('home.availableToday')}
        subtitle={t('home.availableTodaySub')}
        cards={feed?.availableToday}
        href={`/search?availability=Today${search.city ? `&city=${search.city}` : ''}`}
      />

      <Row
        title={t('home.popularBuffets')}
        subtitle={t('home.popularBuffetsSub')}
        cards={feed?.popularBuffets}
        href="/search?type=Buffet&availability=ThisWeek"
      />

      <Row
        title={t('home.popularSetMenus')}
        subtitle={t('home.popularSetMenusSub')}
        cards={feed?.popularSetMenus}
        href="/search?type=SetMenu&availability=ThisWeek"
      />

      {feed && feed.cities.length > 0 && (
        <section className="container section-tight">
          <div className="section-head">
            <div>
              <h2>{t('home.byRegion')}</h2>
              <p>{t('home.byRegionSub')}</p>
            </div>
          </div>
          <div className="card-grid" style={{ gridTemplateColumns: 'repeat(auto-fill, minmax(190px, 1fr))' }}>
            {feed.cities.map((city) => (
              <Link key={city.slug} to={`/search?city=${city.slug}`} className="service-card">
                <div className="service-card-media" style={{ aspectRatio: '3 / 2' }}>
                  {city.imageUrl ? <img src={city.imageUrl} alt="" loading="lazy" /> : null}
                </div>
                <div className="service-card-body">
                  <span className="service-card-title">{isAr ? city.nameAr : city.nameEn}</span>
                  <span className="tiny muted">{t('home.experiences', { count: city.serviceCount })}</span>
                </div>
              </Link>
            ))}
          </div>
        </section>
      )}

      <Row
        title={t('home.featured')}
        subtitle={t('home.featuredSub')}
        cards={feed?.featured}
        href="/search?sort=Rating"
      />

      <section className="container section-tight">
        <div className="section-head"><h2>{t('home.howItWorks')}</h2></div>
        <div className="card-grid" style={{ gridTemplateColumns: 'repeat(auto-fit, minmax(240px, 1fr))' }}>
          {[
            { n: '1', title: t('home.step1'), text: t('home.step1Text'), icon: '🔍' },
            { n: '2', title: t('home.step2'), text: t('home.step2Text'), icon: '⚖️' },
            { n: '3', title: t('home.step3'), text: t('home.step3Text'), icon: '✅' },
          ].map((step) => (
            <div key={step.n} className="card card-pad stack stack-2">
              <span style={{ fontSize: '1.5rem' }} aria-hidden>{step.icon}</span>
              <h3>{step.title}</h3>
              <p className="small muted">{step.text}</p>
            </div>
          ))}
        </div>
      </section>

      <section className="container section">
        <div className="panel row-between wrap" style={{ gap: 'var(--sp-5)' }}>
          <div className="stack stack-2" style={{ maxWidth: '46ch' }}>
            <h2>{t('home.ctaTitle')}</h2>
            <p className="soft">{t('home.ctaText')}</p>
          </div>
          <Link className="btn lg" to="/restaurant/signup">{t('home.ctaAction')}</Link>
        </div>
      </section>
    </>
  )
}

function Row({
  title,
  subtitle,
  cards,
  href,
}: {
  title: string
  subtitle: string
  cards: ServiceCardType[] | undefined
  href: string
}) {
  const { t } = useTranslation()

  // A row with nothing in it is removed rather than left as an empty shelf.
  if (cards && cards.length === 0) return null

  return (
    <section className="container section-tight">
      <div className="section-head">
        <div>
          <h2>{title}</h2>
          <p>{subtitle}</p>
        </div>
        <Link className="btn secondary sm" to={href}>{t('home.seeAll')}</Link>
      </div>

      <div className="rail">
        {cards
          ? cards.map((card) => <ServiceCard key={card.serviceId} card={card} />)
          : Array.from({ length: 4 }, (_, i) => <CardSkeleton key={i} />)}
      </div>
    </section>
  )
}
