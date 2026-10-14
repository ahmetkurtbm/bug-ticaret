import { CouponType, OrderStatus, PaymentStatus, ProductStatus, SellerStatus, ShipmentStatus, type CouponState } from '../api/adminTypes'

export type Tone = 'neutral' | 'success' | 'warning' | 'danger' | 'info'
export type Meta = { label: string; tone: Tone }

export const orderStatusMeta: Record<OrderStatus, Meta> = {
  [OrderStatus.PendingPayment]: { label: 'Ödeme bekleniyor', tone: 'warning' },
  [OrderStatus.Paid]: { label: 'Ödendi', tone: 'info' },
  [OrderStatus.Preparing]: { label: 'Hazırlanıyor', tone: 'info' },
  [OrderStatus.Shipped]: { label: 'Kargoda', tone: 'info' },
  [OrderStatus.Delivered]: { label: 'Teslim edildi', tone: 'success' },
  [OrderStatus.Cancelled]: { label: 'İptal edildi', tone: 'neutral' },
  [OrderStatus.Returned]: { label: 'İade edildi', tone: 'neutral' },
}

export const sellerStatusMeta: Record<SellerStatus, Meta> = {
  [SellerStatus.Pending]: { label: 'Bekliyor', tone: 'warning' },
  [SellerStatus.Active]: { label: 'Aktif', tone: 'success' },
  [SellerStatus.Suspended]: { label: 'Askıda', tone: 'danger' },
  [SellerStatus.Closed]: { label: 'Kapalı', tone: 'neutral' },
}

export const productStatusMeta: Record<ProductStatus, Meta> = {
  [ProductStatus.Draft]: { label: 'Taslak', tone: 'neutral' },
  [ProductStatus.PendingApproval]: { label: 'Onay bekliyor', tone: 'warning' },
  [ProductStatus.Active]: { label: 'Yayında', tone: 'success' },
  [ProductStatus.Rejected]: { label: 'Reddedildi', tone: 'danger' },
  [ProductStatus.Archived]: { label: 'Arşivlendi', tone: 'neutral' },
}

export const paymentStatusMeta: Record<PaymentStatus, Meta> = {
  [PaymentStatus.Pending]: { label: 'Bekliyor', tone: 'warning' },
  [PaymentStatus.Succeeded]: { label: 'Başarılı', tone: 'success' },
  [PaymentStatus.Failed]: { label: 'Başarısız', tone: 'danger' },
  [PaymentStatus.Refunded]: { label: 'İade edildi', tone: 'neutral' },
  [PaymentStatus.PartiallyRefunded]: { label: 'Kısmen iade', tone: 'neutral' },
}

export const shipmentStatusMeta: Record<ShipmentStatus, Meta> = {
  [ShipmentStatus.Created]: { label: 'Oluşturuldu', tone: 'neutral' },
  [ShipmentStatus.PickedUp]: { label: 'Teslim alındı', tone: 'info' },
  [ShipmentStatus.InTransit]: { label: 'Yolda', tone: 'info' },
  [ShipmentStatus.OutForDelivery]: { label: 'Dağıtımda', tone: 'info' },
  [ShipmentStatus.Delivered]: { label: 'Teslim edildi', tone: 'success' },
  [ShipmentStatus.DeliveryFailed]: { label: 'Teslim edilemedi', tone: 'danger' },
  [ShipmentStatus.Returned]: { label: 'İade', tone: 'neutral' },
}

export const couponStateMeta: Record<CouponState, Meta> = {
  running: { label: 'Yürürlükte', tone: 'success' },
  scheduled: { label: 'Planlandı', tone: 'info' },
  expired: { label: 'Süresi doldu', tone: 'neutral' },
  exhausted: { label: 'Limit doldu', tone: 'warning' },
  inactive: { label: 'Pasif', tone: 'neutral' },
}

export const couponTypeLabel: Record<CouponType, string> = {
  [CouponType.Percentage]: 'Yüzde indirim',
  [CouponType.FixedAmount]: 'Sabit tutar',
  [CouponType.FreeShipping]: 'Ücretsiz kargo',
}

const actionLabels: Record<string, string> = {
  'coupon.created': 'Kupon oluşturuldu',
  'coupon.activated': 'Kupon etkinleştirildi',
  'coupon.deactivated': 'Kupon pasifleştirildi',
}

export const campaignActionLabel = (action: string) => actionLabels[action] ?? action
