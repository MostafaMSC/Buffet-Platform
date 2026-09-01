import { createContext, useContext, useState, type ReactNode } from 'react'
import type { AuthResponse } from '../types'

interface AuthState {
  token: string | null
  role: 'RestaurantOwner' | 'Admin' | null
  restaurantId: number | null
}

interface AuthContextValue extends AuthState {
  login: (auth: AuthResponse) => void
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

function readInitialState(): AuthState {
  return {
    token: localStorage.getItem('buffet_token'),
    role: localStorage.getItem('buffet_role') as AuthState['role'],
    restaurantId: (() => {
      const raw = localStorage.getItem('buffet_restaurant_id')
      return raw ? Number(raw) : null
    })(),
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>(readInitialState)

  const login = (auth: AuthResponse) => {
    localStorage.setItem('buffet_token', auth.token)
    localStorage.setItem('buffet_role', auth.role)
    if (auth.restaurantId) {
      localStorage.setItem('buffet_restaurant_id', String(auth.restaurantId))
    } else {
      localStorage.removeItem('buffet_restaurant_id')
    }
    setState({ token: auth.token, role: auth.role, restaurantId: auth.restaurantId })
  }

  const logout = () => {
    localStorage.removeItem('buffet_token')
    localStorage.removeItem('buffet_role')
    localStorage.removeItem('buffet_restaurant_id')
    setState({ token: null, role: null, restaurantId: null })
  }

  return <AuthContext.Provider value={{ ...state, login, logout }}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
