import { OrderStatus } from '../api/types'

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

const dateFormat = new Intl.DateTimeFormat('tr-TR', { dateStyle: 'medium', timeStyle: 'short' })
export const formatDate = (iso: string) => dateFormat.format(new Date(iso))
