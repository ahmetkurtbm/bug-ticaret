import { OrderStatus, ProductStatus } from '../api/types'

export const productStatusLabel: Record<ProductStatus, string> = {
  [ProductStatus.Draft]: 'Taslak',
  [ProductStatus.PendingApproval]: 'Onay bekliyor',
  [ProductStatus.Active]: 'Yayında',
  [ProductStatus.Rejected]: 'Reddedildi',
  [ProductStatus.Archived]: 'Arşivde',
}

export const productStatusTone: Record<ProductStatus, string> = {
  [ProductStatus.Draft]: 'bg-line text-muted',
  [ProductStatus.PendingApproval]: 'bg-amber-100 text-amber-900',
  [ProductStatus.Active]: 'bg-emerald-100 text-emerald-900',
  [ProductStatus.Rejected]: 'bg-red-100 text-red-900',
  [ProductStatus.Archived]: 'bg-line text-muted',
}

export const orderStatusLabel: Record<OrderStatus, string> = {
  [OrderStatus.PendingPayment]: 'Ödeme bekleniyor',
  [OrderStatus.Paid]: 'Ödendi',
  [OrderStatus.Preparing]: 'Hazırlanıyor',
  [OrderStatus.Shipped]: 'Kargoda',
  [OrderStatus.Delivered]: 'Teslim edildi',
  [OrderStatus.Cancelled]: 'İptal edildi',
  [OrderStatus.Returned]: 'İade edildi',
}

export const orderStatusTone: Record<OrderStatus, string> = {
  [OrderStatus.PendingPayment]: 'bg-amber-100 text-amber-900',
  [OrderStatus.Paid]: 'bg-brand-50 text-brand-800',
  [OrderStatus.Preparing]: 'bg-brand-50 text-brand-800',
  [OrderStatus.Shipped]: 'bg-sky-100 text-sky-900',
  [OrderStatus.Delivered]: 'bg-emerald-100 text-emerald-900',
  [OrderStatus.Cancelled]: 'bg-line text-muted',
  [OrderStatus.Returned]: 'bg-line text-muted',
}

const dateTime = new Intl.DateTimeFormat('tr-TR', { dateStyle: 'medium', timeStyle: 'short' })
export const formatDate = (iso: string) => dateTime.format(new Date(iso))

const shortDay = new Intl.DateTimeFormat('tr-TR', { day: 'numeric', month: 'short' })
/** "2026-11-01" -> "1 Kas" */
export const formatDay = (day: string) => shortDay.format(new Date(`${day}T00:00:00`))
