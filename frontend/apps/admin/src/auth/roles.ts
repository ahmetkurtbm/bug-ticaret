import type { AuthUser } from '../api/types'

export const isAdmin = (user: AuthUser | null) => !!user && user.roles.some((role) => role.toLowerCase() === 'admin')
