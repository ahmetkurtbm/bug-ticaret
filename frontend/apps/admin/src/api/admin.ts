import { api } from './client'
import type {
  AdminCoupon,
  AdminOrderDetail,
  AdminOrderListItem,
  AdminProduct,
  AdminSeller,
  CampaignHistoryEntry,
  CreateCouponInput,
  SalesReport,
} from './adminTypes'
import type { PagedResult } from './types'

type QueryValue = string | number | null | undefined

function qs(values: Record<string, QueryValue>) {
  const params = new URLSearchParams()
  for (const [key, value] of Object.entries(values)) {
    if (value !== undefined && value !== null && value !== '') params.set(key, String(value))
  }
  const text = params.toString()
  return text ? `?${text}` : ''
}

export interface OrderFilters {
  q?: string
  status?: number | null
  from?: string
  to?: string
  page: number
  pageSize?: number
}

export const adminApi = {
  sellers: (filters: { status?: number | null; q?: string; page: number }, signal?: AbortSignal) =>
    api<PagedResult<AdminSeller>>(`/api/admin/sellers${qs(filters)}`, { signal }),
  approveSeller: (id: number) => api<AdminSeller>(`/api/admin/sellers/${id}/approve`, { method: 'POST' }),
  suspendSeller: (id: number, note: string) => api<AdminSeller>(`/api/admin/sellers/${id}/suspend`, { method: 'POST', body: { note } }),
  updateCommission: (id: number, commissionRate: number) =>
    api<AdminSeller>(`/api/admin/sellers/${id}/commission`, { method: 'PUT', body: { commissionRate } }),

  products: (filters: { status?: number; q?: string; page: number }, signal?: AbortSignal) =>
    api<PagedResult<AdminProduct>>(`/api/admin/products${qs(filters)}`, { signal }),
  approveProduct: (id: number) => api<void>(`/api/admin/products/${id}/approve`, { method: 'POST' }),
  rejectProduct: (id: number, note: string) => api<void>(`/api/admin/products/${id}/reject`, { method: 'POST', body: { note } }),

  orders: (filters: OrderFilters, signal?: AbortSignal) => api<PagedResult<AdminOrderListItem>>(`/api/admin/orders${qs({ ...filters })}`, { signal }),
  order: (orderNumber: string, signal?: AbortSignal) => api<AdminOrderDetail>(`/api/admin/orders/${encodeURIComponent(orderNumber)}`, { signal }),

  coupons: (signal?: AbortSignal) => api<AdminCoupon[]>('/api/admin/coupons', { signal }),
  createCoupon: (input: CreateCouponInput) => api<AdminCoupon>('/api/admin/coupons', { method: 'POST', body: input }),
  setCouponActive: (id: number, active: boolean) => api<AdminCoupon>(`/api/admin/coupons/${id}/${active ? 'activate' : 'deactivate'}`, { method: 'POST' }),
  campaignHistory: (take = 50, signal?: AbortSignal) => api<CampaignHistoryEntry[]>(`/api/admin/campaign-history${qs({ take })}`, { signal }),

  salesReport: (from: string, to: string, signal?: AbortSignal) => api<SalesReport>(`/api/admin/reports/sales${qs({ from, to })}`, { signal }),
}
