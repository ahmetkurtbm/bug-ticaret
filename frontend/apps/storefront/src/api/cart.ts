import { api } from './client'
import type { Cart } from './types'

export const cartApi = {
  get: (signal?: AbortSignal) => api<Cart>('/api/cart', { signal }),
  add: (variantId: number, quantity: number) => api<Cart>('/api/cart/items', { method: 'POST', body: { variantId, quantity } }),
  update: (itemId: number, quantity: number) => api<Cart>(`/api/cart/items/${itemId}`, { method: 'PUT', body: { quantity } }),
  remove: (itemId: number) => api<Cart>(`/api/cart/items/${itemId}`, { method: 'DELETE' }),
  applyCoupon: (couponCode: string) => api<Cart>('/api/cart/coupon', { method: 'PUT', body: { couponCode } }),
  removeCoupon: () => api<Cart>('/api/cart/coupon', { method: 'DELETE' }),
}
