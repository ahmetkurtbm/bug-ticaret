// Bölüm FE-01 kontrolü: ürün sayfası yüklenince hatasız çizilmeli.
import { screen } from '@testing-library/react'
import { expect, test, vi } from 'vitest'
import { catalogApi } from '../api/catalog'
import type { ProductDetail } from '../api/types'
import { ProductPage } from '../pages/ProductPage'
import { renderAt } from './test-utils'

const product: ProductDetail = {
  id: 42,
  name: 'Deneme Keten Gömlek',
  slug: 'deneme-keten-gomlek-p42',
  description: '<p>Yazlık</p>',
  brandName: null,
  seller: { id: 1, storeName: 'Tezgah Demo Mağaza', slug: 'tezgah-demo-magaza', rating: 4.5, ratingCount: 10 },
  breadcrumbs: [{ name: 'Erkek', slug: 'erkek' }],
  images: ['https://picsum.photos/seed/a/600/600'],
  variants: [
    { id: 1, sku: 'DNM-1', color: 'Beyaz', size: 'M', price: 499.9, listPrice: 599.9, availableQuantity: 3 },
    { id: 2, sku: 'DNM-2', color: 'Beyaz', size: 'L', price: 499.9, listPrice: null, availableQuantity: 0 },
  ],
  attributes: [],
  rating: 4.2,
  reviewCount: 7,
}

test('ürün sayfası yükleniyor durumundan ürün detayına hatasız geçer', async () => {
  vi.spyOn(catalogApi, 'product').mockImplementation(() => new Promise((resolve) => setTimeout(() => resolve(product), 20)))
  vi.spyOn(console, 'error').mockImplementation(() => {})

  renderAt('/urun/deneme-keten-gomlek-p42', '/urun/:slug', <ProductPage />)

  expect(await screen.findByRole('heading', { name: product.name })).toBeTruthy()
  expect(screen.getByRole('button', { name: /sepete ekle/i })).toBeTruthy()
})
