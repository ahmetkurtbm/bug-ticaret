// Bölüm 21 kontrolü: ödeme sayfasında eklenen adres listede hemen görünmeli.
import { fireEvent, screen } from '@testing-library/react'
import { expect, test, vi } from 'vitest'
import { cartApi } from '../api/cart'
import { addressApi } from '../api/orders'
import type { Address, Cart } from '../api/types'
import { CheckoutPage } from '../pages/CheckoutPage'
import { renderAt } from './test-utils'

const cart = {
  lines: [{ id: 1, variantId: 1, productId: 1, productName: 'Deneme Ürün', productSlug: 'deneme-p1', variantLabel: null, imageUrl: null, sellerName: 'Mağaza', unitPrice: 100, priceChanged: false, quantity: 1, availableQuantity: 5, discountAmount: 0, lineTotal: 100 }],
  itemCount: 1, subtotal: 100, discountTotal: 0, shippingTotal: 49.9, vatTotal: 16.67, grandTotal: 149.9, couponCode: null, couponError: null, freeShippingRemaining: 400,
} as Cart
const home: Address = { id: 1, title: 'Ev', fullName: 'Deneme Müşteri', phone: '05321234567', city: 'İstanbul', district: 'Kadıköy', line: 'Moda Caddesi No 10', postalCode: null, isDefault: true }
const office: Address = { ...home, id: 2, title: 'Ofis', line: 'Levent Caddesi No 5 Kat 3', isDefault: false }

test('ödeme sayfasında eklenen yeni adres listede görünür', async () => {
  vi.spyOn(cartApi, 'get').mockResolvedValue(cart)
  const saved: Address[] = [home]
  vi.spyOn(addressApi, 'list').mockImplementation(async () => [...saved])
  vi.spyOn(addressApi, 'create').mockImplementation(async () => {
    saved.push(office)
    return office
  })

  renderAt('/odeme', '/odeme', <CheckoutPage />)
  fireEvent.click(await screen.findByRole('button', { name: 'Yeni adres ekle' }))
  const fill = (label: string, value: string) => fireEvent.change(screen.getByLabelText(label), { target: { value } })
  fill('Adres başlığı', 'Ofis')
  fill('Ad soyad', 'Deneme Müşteri')
  fill('Cep telefonu', '05321234567')
  fill('İl', 'İstanbul')
  fill('İlçe', 'Beşiktaş')
  fill('Açık adres', 'Levent Caddesi No 5 Kat 3')
  fireEvent.click(screen.getByRole('button', { name: 'Adresi kaydet' }))

  expect(await screen.findByText('Levent Caddesi No 5 Kat 3')).toBeTruthy()
})
