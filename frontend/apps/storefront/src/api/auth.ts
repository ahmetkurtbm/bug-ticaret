import { api } from './client'
import type { AuthResponse } from './types'

export interface RegisterInput {
  email: string
  password: string
  firstName: string
  lastName: string
}

export const authApi = {
  login: (email: string, password: string) => api<AuthResponse>('/api/auth/login', { method: 'POST', body: { email, password } }),
  register: (input: RegisterInput) => api<AuthResponse>('/api/auth/register', { method: 'POST', body: input }),
  logout: (refreshToken: string) => api<void>('/api/auth/logout', { method: 'POST', body: { refreshToken } }),
}
