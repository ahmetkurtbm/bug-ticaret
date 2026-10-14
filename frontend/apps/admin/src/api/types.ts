// Ortak DTO'lar (Tezgah.Application). Alan adları camelCase. Uygulamaya özel tipler aşağıya eklenir.

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface AuthUser {
  id: number
  email: string
  firstName: string
  lastName: string
  roles: string[]
  sellerId: number | null
}

export interface AuthResponse {
  accessToken: string
  accessTokenExpiresAt: string
  refreshToken: string
  user: AuthUser
}

/** ASP.NET Core ProblemDetails / ValidationProblemDetails. */
export interface ProblemDetails {
  status?: number
  title?: string
  detail?: string
  code?: string
  errors?: Record<string, string[]>
}
