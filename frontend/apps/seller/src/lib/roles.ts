import type { AuthUser } from '../api/types'

export const hasSellerRole = (user: AuthUser | null) => !!user?.roles.some((r) => r.toLowerCase() === 'seller')
