// Bölüm 32 kontrolü: başka bir sekme oturumu yenilediyse bu sekme oturumu kapatmamalı.
import { expect, test, vi } from 'vitest'
import type { AuthResponse } from '../api/types'
import { api } from '../api/client'
import { tokenStore } from '../auth/tokenStore'

const session = (n: number): AuthResponse => ({
  accessToken: `erisim-${n}`,
  accessTokenExpiresAt: '2030-01-01T00:00:00Z',
  refreshToken: `yenileme-${n}`,
  user: { id: 7, email: 'musteri@tezgah.test', firstName: 'Deneme', lastName: 'Müşteri', roles: ['customer'], sellerId: null },
})

const json = (status: number, body: unknown) => new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })

test('iki sekme açıkken erişim süresi dolunca oturum kapanmaz', async () => {
  tokenStore.set(session(1))
  vi.stubGlobal('fetch', vi.fn(async (url: string, init?: RequestInit) => {
    if (url.endsWith('/api/auth/refresh')) {
      // Diğer sekme bir an önce yeniledi: depoda 2. oturum var, 1. yenileme anahtarı artık geçersiz.
      localStorage.setItem('tezgah.session', JSON.stringify(session(2)))
      window.dispatchEvent(new StorageEvent('storage', { key: 'tezgah.session' }))
      return json(401, { title: 'Oturumun süresi doldu.', code: 'invalid_refresh_token' })
    }
    const auth = (init?.headers as Record<string, string> | undefined)?.Authorization
    return auth === 'Bearer erisim-2' ? json(200, { ok: true }) : json(401, { title: 'Yetkisiz' })
  }))

  await expect(api('/api/me/addresses')).resolves.toEqual({ ok: true })
  expect(tokenStore.get()?.refreshToken).toBe('yenileme-2')
})
