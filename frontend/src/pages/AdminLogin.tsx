import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { api } from '../api/client'
import { useAuth } from '../context/AuthContext'
import type { AuthResponse } from '../types'

export function AdminLogin() {
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
      if (res.data.role !== 'Admin') {
        setError(true)
        return
      }
      login(res.data)
      navigate('/admin')
    } catch {
      setError(true)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="container section auth-shell">
      <form className="card card-pad stack stack-4 auth-card" onSubmit={handleSubmit}>
        <h1 style={{ fontSize: '1.5rem' }}>{t('auth.adminLoginTitle')}</h1>

        {error && <div className="alert bad">{t('auth.error')}</div>}

        <label className="field">
          <span>{t('auth.phone')}</span>
          <input type="tel" required autoComplete="username" value={phoneNumber} onChange={(e) => setPhoneNumber(e.target.value)} />
        </label>

        <label className="field">
          <span>{t('auth.password')}</span>
          <input type="password" required autoComplete="current-password" value={password} onChange={(e) => setPassword(e.target.value)} />
        </label>

        <button className="btn lg" type="submit" disabled={submitting}>
          {submitting ? t('common.loading') : t('auth.login')}
        </button>
      </form>
    </div>
  )
}
