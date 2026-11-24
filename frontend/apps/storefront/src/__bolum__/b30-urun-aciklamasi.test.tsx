// Bölüm 30 kontrolü: satıcının yazdığı açıklama sayfada kod olarak çalışmamalı.
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
  description: '<b>Yazlık</b> gömlek <img src="x" onerror="window.__acik = true"><script>window.__acik = true</script>',
  brandName: null,
  seller: { id: 1, storeName: 'Tezgah Demo Mağaza', slug: 'tezgah-demo-magaza', rating: 4.5, ratingCount: 10 },
  breadcrumbs: [{ name: 'Erkek', slug: 'erkek' }],
  images: ['https://picsum.photos/seed/a/600/600'],
  variants: [{ id: 1, sku: 'DNM-1', color: 'Beyaz', size: 'M', price: 499.9, listPrice: null, availableQuantity: 3 }],
  attributes: [],
  rating: 4.2,
  reviewCount: 7,
}

test('ürün açıklamasındaki etiketler sayfada öğe ya da kod olarak yer almaz', async () => {
  vi.spyOn(catalogApi, 'product').mockResolvedValue(product)

  const { container } = renderAt('/urun/deneme-keten-gomlek-p42', '/urun/:slug', <ProductPage />)
  const heading = await screen.findByRole('heading', { name: 'Ürün açıklaması' })
  const section = heading.closest('section')!

  expect(section.textContent).toContain('Yazlık')
  expect(section.querySelector('img, script, [onerror]')).toBeNull()
  expect(container.querySelector('[onerror]')).toBeNull()
})
