import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router-dom'
import { api } from '../api/client'
import { useAuth } from '../context/AuthContext'
import { AreaSelect } from '../components/AreaSelect'
import type { AuthResponse } from '../types'

export function RestaurantSignup() {
  const { t } = useTranslation()
  const { login } = useAuth()
  const navigate = useNavigate()
  const [phoneNumber, setPhoneNumber] = useState('')
  const [password, setPassword] = useState('')
  const [restaurantName, setRestaurantName] = useState('')
  const [restaurantNameAr, setRestaurantNameAr] = useState('')
  const [areaId, setAreaId] = useState<number | ''>('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!areaId) return
    setSubmitting(true)
    setError(null)
    try {
      const res = await api.post<AuthResponse>('/auth/signup', {
        phoneNumber,
        password,
        restaurantName,
        restaurantNameAr,
        areaId,
      })
      login(res.data)
      navigate('/dashboard')
    } catch (err: unknown) {
      const message =
        (err as { response?: { data?: { message?: string } } })?.response?.data?.message ??
        t('auth.error')
      setError(message)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="container section auth-shell">
      <form className="card card-pad stack stack-4 auth-card" onSubmit={handleSubmit}>
        <div className="stack stack-1">
          <h1 style={{ fontSize: '1.5rem' }}>{t('auth.signupTitle')}</h1>
          <p className="small soft">{t('auth.signupSubtitle')}</p>
        </div>

        {error && <div className="alert bad">{error}</div>}

        <label className="field">
          <span>{t('auth.restaurantName')}</span>
          <input required value={restaurantName} onChange={(e) => setRestaurantName(e.target.value)} />
        </label>

        <label className="field">
          <span>{t('auth.restaurantNameAr')}</span>
          <input required dir="rtl" value={restaurantNameAr} onChange={(e) => setRestaurantNameAr(e.target.value)} />
        </label>

        <label className="field">
          <span>{t('auth.area')}</span>
          <AreaSelect required value={areaId} onChange={setAreaId} />
        </label>

        <label className="field">
          <span>{t('auth.phone')}</span>
          <input type="tel" required autoComplete="username" value={phoneNumber} onChange={(e) => setPhoneNumber(e.target.value)} />
        </label>

        <label className="field">
          <span>{t('auth.password')}</span>
          <input
            type="password"
            required
            minLength={6}
            autoComplete="new-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
        </label>

        <button className="btn lg" type="submit" disabled={submitting}>
          {submitting ? t('common.loading') : t('auth.signup')}
        </button>

        <p className="small muted" style={{ textAlign: 'center' }}>
          <Link to="/restaurant/login">{t('auth.alreadyHaveAccount')}</Link>
        </p>
      </form>
    </div>
  )
}
