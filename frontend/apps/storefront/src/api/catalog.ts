import { api } from './client'
import type { CategoryNode, PagedResult, ProductCard, ProductDetail, ProductSearchParams, SellerPublic } from './types'

export const catalogApi = {
  categories: (signal?: AbortSignal) => api<CategoryNode[]>('/api/categories', { signal }),

  search: (params: ProductSearchParams, signal?: AbortSignal) => {
    const query = new URLSearchParams()
    for (const [key, value] of Object.entries(params)) {
      if (value !== undefined && value !== '' && value !== null) query.set(key, String(value))
    }
    return api<PagedResult<ProductCard>>(`/api/products?${query}`, { signal })
  },

  seller: (slug: string, signal?: AbortSignal) => api<SellerPublic>(`/api/sellers/${encodeURIComponent(slug)}`, { signal }),

  product: (slug: string, signal?: AbortSignal) => api<ProductDetail>(`/api/products/${encodeURIComponent(slug)}`, { signal }),
}
