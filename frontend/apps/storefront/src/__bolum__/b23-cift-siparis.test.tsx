// Bölüm 23 kontrolü: ödeme düğmesine art arda basmak ikinci bir sipariş açmamalı.
import { fireEvent, screen } from '@testing-library/react'
import { expect, test, vi } from 'vitest'
import { cartApi } from '../api/cart'
import { addressApi, orderApi } from '../api/orders'
import type { Cart } from '../api/types'
import { CheckoutPage } from '../pages/CheckoutPage'
import { renderAt } from './test-utils'

const cart = {
  lines: [{ id: 1, variantId: 1, productId: 1, productName: 'Deneme Ürün', productSlug: 'deneme-p1', variantLabel: null, imageUrl: null, sellerName: 'Mağaza', unitPrice: 100, priceChanged: false, quantity: 1, availableQuantity: 5, discountAmount: 0, lineTotal: 100 }],
  itemCount: 1, subtotal: 100, discountTotal: 0, shippingTotal: 49.9, vatTotal: 16.67, grandTotal: 149.9, couponCode: null, couponError: null, freeShippingRemaining: 400,
} as Cart

test('ödeme düğmesine iki kez basınca tek sipariş isteği (aynı anahtarla) gider', async () => {
  vi.spyOn(cartApi, 'get').mockResolvedValue(cart)
  vi.spyOn(addressApi, 'list').mockResolvedValue([
    { id: 1, title: 'Ev', fullName: 'Deneme', phone: '05321234567', city: 'İstanbul', district: 'Kadıköy', line: 'Moda Caddesi No 10', postalCode: null, isDefault: true },
  ])
  // Sunucu yavaş: istek bitmeden kullanıcı tekrar basıyor.
  const place = vi.spyOn(orderApi, 'place').mockImplementation(() => new Promise(() => {}))

  renderAt('/odeme', '/odeme', <CheckoutPage />)
  const button = await screen.findByRole('button', { name: /ödemeye geç/i })
  fireEvent.click(button)
  fireEvent.click(button)
  fireEvent.click(button)
  await new Promise((resolve) => setTimeout(resolve, 50))

  const keys = new Set(place.mock.calls.map((call) => call[2]))
  expect(place.mock.calls.length).toBeGreaterThan(0)
  expect(keys.size).toBe(1)
})
