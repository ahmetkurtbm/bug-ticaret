import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router'
import { useAuth } from '../auth/AuthContext'
import { isAdmin } from '../auth/roles'
import { AccessDenied } from './AccessDenied'

export function RequireAdmin({ children }: { children: ReactNode }) {
  const { user } = useAuth()
  const location = useLocation()
  if (!user) return <Navigate to="/giris" replace state={{ from: location.pathname + location.search }} />
  if (!isAdmin(user)) return <AccessDenied />
  return children
}
