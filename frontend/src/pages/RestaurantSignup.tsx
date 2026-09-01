import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router-dom'
import { api } from '../api/client'
import { useAuth } from '../context/AuthContext'
import type { Area, AuthResponse } from '../types'

export function RestaurantSignup() {
  const { t, i18n } = useTranslation()
  const isAr = i18n.language === 'ar'
  const { login } = useAuth()
  const navigate = useNavigate()

  const [areas, setAreas] = useState<Area[]>([])
  const [phoneNumber, setPhoneNumber] = useState('')
  const [password, setPassword] = useState('')
  const [restaurantName, setRestaurantName] = useState('')
  const [restaurantNameAr, setRestaurantNameAr] = useState('')
  const [areaId, setAreaId] = useState<number | ''>('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    api.get<Area[]>('/areas').then((res) => setAreas(res.data))
  }, [])

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
    <div className="container">
      <form className="form-card" onSubmit={handleSubmit}>
        <h1>{t('auth.signupTitle')}</h1>
        <p className="subtitle">{t('auth.signupSubtitle')}</p>
        {error && <div className="form-error">{error}</div>}

        <div className="form-field">
          <label>{t('auth.restaurantName')}</label>
          <input required value={restaurantName} onChange={(e) => setRestaurantName(e.target.value)} />
        </div>
        <div className="form-field">
          <label>{t('auth.restaurantNameAr')}</label>
          <input
            required
            dir="rtl"
            value={restaurantNameAr}
            onChange={(e) => setRestaurantNameAr(e.target.value)}
          />
        </div>
        <div className="form-field">
          <label>{t('auth.area')}</label>
          <select required value={areaId} onChange={(e) => setAreaId(Number(e.target.value))}>
            <option value="">—</option>
            {areas.map((a) => (
              <option key={a.id} value={a.id}>
                {isAr ? a.nameAr : a.nameEn}
              </option>
            ))}
          </select>
        </div>
        <div className="form-field">
          <label>{t('auth.phone')}</label>
          <input type="tel" required value={phoneNumber} onChange={(e) => setPhoneNumber(e.target.value)} />
        </div>
        <div className="form-field">
          <label>{t('auth.password')}</label>
          <input
            type="password"
            required
            minLength={6}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
        </div>

        <button className="btn" type="submit" disabled={submitting}>
          {t('auth.signup')}
        </button>
        <div className="form-footer">
          <Link to="/restaurant/login">{t('auth.alreadyHaveAccount')}</Link>
        </div>
      </form>
    </div>
  )
}
