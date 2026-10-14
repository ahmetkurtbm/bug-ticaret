// Bölüm API-02 kontrolü: kupon isteği backend'in beklediği alan adıyla gitmeli.
import { expect, test, vi } from 'vitest'
import { cartApi } from '../api/cart'

test('kupon kodu "couponCode" alanında gönderilir', async () => {
  const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(JSON.stringify({ lines: [] }), { status: 200 }))

  await cartApi.applyCoupon('KARGOBEDAVA')

  const [url, init] = fetchMock.mock.calls[0]
  expect(String(url)).toContain('/api/cart/coupon')
  expect(JSON.parse(String(init?.body))).toEqual({ couponCode: 'KARGOBEDAVA' })
})
