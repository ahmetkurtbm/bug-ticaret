import { useQueryClient } from '@tanstack/react-query'
import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { authApi, type RegisterInput } from '../api/auth'
import type { AuthUser } from '../api/types'
import { tokenStore } from './tokenStore'

interface AuthContextValue {
  user: AuthUser | null
  login: (email: string, password: string) => Promise<void>
  register: (input: RegisterInput) => Promise<void>
  logout: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient()
  const [user, setUser] = useState<AuthUser | null>(() => tokenStore.get()?.user ?? null)

  useEffect(() => tokenStore.subscribe((session) => setUser(session?.user ?? null)), [])

  const login = useCallback(
    async (email: string, password: string) => {
      tokenStore.set(await authApi.login(email, password))
      // Ziyaretçi sepeti sunucuda kullanıcının sepetine taşındı; sepeti yeniden çek.
      await queryClient.invalidateQueries({ queryKey: ['cart'] })
    },
    [queryClient],
  )

  const register = useCallback(
    async (input: RegisterInput) => {
      tokenStore.set(await authApi.register(input))
      await queryClient.invalidateQueries({ queryKey: ['cart'] })
    },
    [queryClient],
  )

  const logout = useCallback(async () => {
    const refreshToken = tokenStore.get()?.refreshToken
    tokenStore.clear()
    if (refreshToken) await authApi.logout(refreshToken).catch(() => undefined)
    queryClient.removeQueries({ queryKey: ['cart'] })
  }, [queryClient])

  const value = useMemo(() => ({ user, login, register, logout }), [user, login, register, logout])
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth, AuthProvider içinde kullanılmalı')
  return ctx
}
