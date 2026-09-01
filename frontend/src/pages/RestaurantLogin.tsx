import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router-dom'
import { api } from '../api/client'
import { useAuth } from '../context/AuthContext'
import type { AuthResponse } from '../types'

export function RestaurantLogin() {
  const { t } = useTranslation()
  const { login } = useAuth()
  const navigate = useNavigate()
  const [phoneNumber, setPhoneNumber] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState(false)
  const [submitting, setSubmitting] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setSubmitting(true)
    setError(false)
    try {
      const res = await api.post<AuthResponse>('/auth/login', { phoneNumber, password })
      login(res.data)
      navigate(res.data.role === 'Admin' ? '/admin' : '/dashboard')
    } catch {
      setError(true)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="container">
      <form className="form-card" onSubmit={handleSubmit}>
        <h1>{t('auth.loginTitle')}</h1>
        {error && <div className="form-error">{t('auth.error')}</div>}
        <div className="form-field">
          <label>{t('auth.phone')}</label>
          <input
            type="tel"
            required
            value={phoneNumber}
            onChange={(e) => setPhoneNumber(e.target.value)}
          />
        </div>
        <div className="form-field">
          <label>{t('auth.password')}</label>
          <input
            type="password"
            required
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
        </div>
        <button className="btn" type="submit" disabled={submitting}>
          {t('auth.login')}
        </button>
        <div className="form-footer">
          <Link to="/restaurant/signup">{t('auth.needAccount')}</Link>
        </div>
      </form>
    </div>
  )
}
