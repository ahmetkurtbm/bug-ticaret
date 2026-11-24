import type { AuthResponse, ProblemDetails } from './types'
import { tokenStore } from '../auth/tokenStore'

const API_URL = import.meta.env.VITE_API_URL
const CART_ID_KEY = 'tezgah.cartId'

export class ApiError extends Error {
  readonly status: number
  readonly code?: string
  readonly fieldErrors: Record<string, string>

  constructor(status: number, problem: ProblemDetails | null) {
    super(problem?.title ?? problem?.detail ?? 'Beklenmeyen bir hata oluştu.')
    this.status = status
    this.code = problem?.code
    // ValidationProblemDetails alan adlarını PascalCase döner ("Quantity"); formlar camelCase kullanır.
    this.fieldErrors = Object.fromEntries(
      Object.entries(problem?.errors ?? {}).map(([key, messages]) => [key.charAt(0).toLowerCase() + key.slice(1), messages[0]]),
    )
  }
}

/** Giriş yapmamış ziyaretçinin sepet kimliği; ilk ihtiyaçta üretilir. */
export function getCartId(): string {
  let id = localStorage.getItem(CART_ID_KEY)
  if (!id) {
    id = crypto.randomUUID()
    localStorage.setItem(CART_ID_KEY, id)
  }
  return id
}

// Aynı anda birden fazla istek 401 alırsa yenileme tek kez yapılır, diğerleri onu bekler.
let refreshInFlight: Promise<boolean> | null = null

async function refreshTokens(): Promise<boolean> {
  const refreshToken = tokenStore.get()?.refreshToken
  if (!refreshToken) return false

  refreshInFlight ??= (async () => {
    try {
      const response = await fetch(`${API_URL}/api/auth/refresh`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken }),
      })
      if (!response.ok) {
        tokenStore.clear()
        return false
      }
      tokenStore.set((await response.json()) as AuthResponse)
      return true
    } finally {
      refreshInFlight = null
    }
  })()
  return refreshInFlight
}

export interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE'
  body?: unknown
  signal?: AbortSignal
  headers?: Record<string, string>
}

export async function api<T>(path: string, options: RequestOptions = {}, retried = false): Promise<T> {
  const headers: Record<string, string> = { 'X-Cart-Id': getCartId(), ...options.headers }
  if (options.body !== undefined) headers['Content-Type'] = 'application/json'
  const session = tokenStore.get()
  if (session) headers.Authorization = `Bearer ${session.accessToken}`

  const response = await fetch(`${API_URL}${path}`, {
    method: options.method ?? 'GET',
    headers,
    body: options.body === undefined ? undefined : JSON.stringify(options.body),
    signal: options.signal,
  })

  if (response.status === 401 && session && !retried && (await refreshTokens())) {
    return api<T>(path, options, true)
  }

  if (!response.ok) {
    const problem = (await response.json().catch(() => null)) as ProblemDetails | null
    throw new ApiError(response.status, problem)
  }

  if (response.status === 204) return undefined as T
  return (await response.json()) as T
}
