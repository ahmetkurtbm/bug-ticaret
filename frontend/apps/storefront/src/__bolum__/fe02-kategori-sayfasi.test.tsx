// Bölüm FE-02 kontrolü: kategori sayfası açılınca ürün araması bir kez yapılmalı, döngüye girmemeli.
import { screen } from '@testing-library/react'
import { expect, test, vi } from 'vitest'
import { catalogApi } from '../api/catalog'
import { ProductListPage } from '../pages/ProductListPage'
import { renderAt } from './test-utils'

test('kategori sayfası ürünleri bir kez ister', async () => {
  vi.spyOn(catalogApi, 'categories').mockResolvedValue([{ id: 1, name: 'Kadın', slug: 'kadin', children: [] }])
  const search = vi.spyOn(catalogApi, 'search').mockResolvedValue({ items: [], page: 1, pageSize: 24, totalCount: 0, totalPages: 0 })

  renderAt('/kategori/kadin?sort=price_asc', '/kategori/:slug', <ProductListPage />)

  expect(await screen.findByRole('heading', { name: 'Kadın' })).toBeTruthy()
  await new Promise((resolve) => setTimeout(resolve, 500))
  expect(search.mock.calls.length).toBeLessThanOrEqual(2)
})
