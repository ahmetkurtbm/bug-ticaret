import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router'
import { useAuth } from '../auth/AuthContext'

/** Giriş gerektiren sayfalar. Girişten sonra kullanıcı geldiği adrese döner. */
export function RequireAuth({ children }: { children: ReactNode }) {
  const { user } = useAuth()
  const location = useLocation()
  if (!user) return <Navigate to="/giris" replace state={{ from: location.pathname + location.search }} />
  return children
}
