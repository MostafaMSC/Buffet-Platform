import type { ReactNode } from 'react'
import { Navigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

export function ProtectedRoute({
  role,
  children,
}: {
  role: 'RestaurantOwner' | 'Admin'
  children: ReactNode
}) {
  const { token, role: currentRole } = useAuth()

  if (!token || currentRole !== role) {
    return <Navigate to={role === 'Admin' ? '/admin/login' : '/restaurant/login'} replace />
  }

  return <>{children}</>
}
