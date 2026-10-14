import { api } from './client'
import type { Address, NewAddress, OrderDetail, OrderSummary, PagedResult, PlaceOrderResponse } from './types'

export const addressApi = {
  list: (signal?: AbortSignal) => api<Address[]>('/api/me/addresses', { signal }),
  create: (address: NewAddress) => api<Address>('/api/me/addresses', { method: 'POST', body: address }),
}

export const orderApi = {
  /** idempotencyKey aynı ödeme denemesi için sabit kalmalı; çift tıklama ikinci sipariş açmaz. */
  place: (addressId: number, installmentCount: number, idempotencyKey: string) =>
    api<PlaceOrderResponse>('/api/orders', {
      method: 'POST',
      body: { addressId, installmentCount },
      headers: { 'Idempotency-Key': idempotencyKey },
    }),
  retryPayment: (orderNumber: string) => api<PlaceOrderResponse>(`/api/orders/${encodeURIComponent(orderNumber)}/payment`, { method: 'POST' }),
  list: (page: number, signal?: AbortSignal) => api<PagedResult<OrderSummary>>(`/api/orders?page=${page}&pageSize=10`, { signal }),
  get: (orderNumber: string, signal?: AbortSignal) => api<OrderDetail>(`/api/orders/${encodeURIComponent(orderNumber)}`, { signal }),
}
