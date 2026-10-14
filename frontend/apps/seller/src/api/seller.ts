import { api } from './client'
import type {
  CategoryNode,
  CreateProductInput,
  OrderFilter,
  PagedResult,
  ProductStatus,
  SellerApplicationInput,
  SellerDashboard,
  SellerOrder,
  SellerProductDetail,
  SellerProductListItem,
  SellerProfile,
  SellerVariant,
  UpdateProductInput,
} from './types'

function query(params: Record<string, string | number | undefined>) {
  const search = new URLSearchParams()
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== '') search.set(key, String(value))
  }
  const text = search.toString()
  return text ? `?${text}` : ''
}

export const sellerApi = {
  // 204 (başvuru yok) null'a çevrilir: TanStack Query undefined veriyi kabul etmez.
  myApplication: async (signal?: AbortSignal) => (await api<SellerProfile | undefined>('/api/seller-applications/mine', { signal })) ?? null,
  apply: (input: SellerApplicationInput) => api<SellerProfile>('/api/seller-applications', { method: 'POST', body: input }),

  me: (signal?: AbortSignal) => api<SellerProfile>('/api/seller/me', { signal }),
  dashboard: (signal?: AbortSignal) => api<SellerDashboard>('/api/seller/dashboard', { signal }),
  categories: (signal?: AbortSignal) => api<CategoryNode[]>('/api/categories', { signal }),

  products: (params: { q?: string; status?: ProductStatus; page?: number; pageSize?: number }, signal?: AbortSignal) =>
    api<PagedResult<SellerProductListItem>>(`/api/seller/products${query(params)}`, { signal }),
  product: (id: number, signal?: AbortSignal) => api<SellerProductDetail>(`/api/seller/products/${id}`, { signal }),
  createProduct: (input: CreateProductInput) => api<SellerProductDetail>('/api/seller/products', { method: 'POST', body: input }),
  updateProduct: (id: number, input: UpdateProductInput) => api<SellerProductDetail>(`/api/seller/products/${id}`, { method: 'PUT', body: input }),
  submitProduct: (id: number) => api<SellerProductDetail>(`/api/seller/products/${id}/submit`, { method: 'POST' }),
  archiveProduct: (id: number) => api<SellerProductDetail>(`/api/seller/products/${id}/archive`, { method: 'POST' }),
  updateVariant: (id: number, body: { price: number; listPrice: number | null; isActive: boolean }) =>
    api<SellerVariant>(`/api/seller/variants/${id}`, { method: 'PUT', body }),
  setStock: (id: number, body: { quantity: number; note: string | null }) =>
    api<SellerVariant>(`/api/seller/variants/${id}/stock`, { method: 'PUT', body }),

  orders: (params: { filter: OrderFilter; page?: number; pageSize?: number }, signal?: AbortSignal) =>
    api<PagedResult<SellerOrder>>(`/api/seller/orders${query(params)}`, { signal }),
  ship: (orderNumber: string, body: { carrier: string; trackingNumber: string }) =>
    api<SellerOrder>(`/api/seller/orders/${encodeURIComponent(orderNumber)}/ship`, { method: 'POST', body }),
}
