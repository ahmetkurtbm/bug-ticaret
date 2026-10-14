// Yönetim paneli DTO'ları (Tezgah.Application.Admin / Reporting). Alan adları camelCase, enum'lar sayı.

/** Tezgah.Domain.Sellers.SellerStatus ile aynı sayılar. */
export const SellerStatus = { Pending: 1, Active: 2, Suspended: 3, Closed: 4 } as const
export type SellerStatus = (typeof SellerStatus)[keyof typeof SellerStatus]

/** Tezgah.Domain.Catalog.ProductStatus ile aynı sayılar. */
export const ProductStatus = { Draft: 1, PendingApproval: 2, Active: 3, Rejected: 4, Archived: 5 } as const
export type ProductStatus = (typeof ProductStatus)[keyof typeof ProductStatus]

/** Tezgah.Domain.Orders.OrderStatus ile aynı sayılar. */
export const OrderStatus = { PendingPayment: 1, Paid: 2, Preparing: 3, Shipped: 4, Delivered: 5, Cancelled: 6, Returned: 7 } as const
export type OrderStatus = (typeof OrderStatus)[keyof typeof OrderStatus]

/** Tezgah.Domain.Campaigns.CouponType ile aynı sayılar. */
export const CouponType = { Percentage: 1, FixedAmount: 2, FreeShipping: 3 } as const
export type CouponType = (typeof CouponType)[keyof typeof CouponType]

/** Tezgah.Domain.Payments.PaymentStatus ile aynı sayılar. */
export const PaymentStatus = { Pending: 1, Succeeded: 2, Failed: 3, Refunded: 4, PartiallyRefunded: 5 } as const
export type PaymentStatus = (typeof PaymentStatus)[keyof typeof PaymentStatus]

/** Tezgah.Domain.Shipping.ShipmentStatus ile aynı sayılar. */
export const ShipmentStatus = { Created: 1, PickedUp: 2, InTransit: 3, OutForDelivery: 4, Delivered: 5, DeliveryFailed: 6, Returned: 7 } as const
export type ShipmentStatus = (typeof ShipmentStatus)[keyof typeof ShipmentStatus]

export interface AdminSeller {
  id: number
  storeName: string
  slug: string
  legalName: string
  taxNumber: string
  city: string
  status: SellerStatus
  commissionRate: number
  ownerEmail: string
  ownerName: string
  productCount: number
  createdAt: string
  approvedAt: string | null
}

export interface AdminProduct {
  id: number
  name: string
  slug: string
  imageUrl: string | null
  sellerName: string
  categoryName: string
  status: ProductStatus
  variantCount: number
  minPrice: number
  createdAt: string
  updatedAt: string | null
}

export interface AdminOrderListItem {
  id: number
  orderNumber: string
  customerEmail: string
  customerName: string
  placedAt: string
  status: OrderStatus
  grandTotal: number
  itemCount: number
  sellerCount: number
  couponCode: string | null
}

export interface AdminOrderItem {
  productName: string
  sku: string
  variantLabel: string | null
  sellerName: string
  quantity: number
  unitPrice: number
  discountAmount: number
  lineTotal: number
  commissionRate: number
}

export interface AdminPayment {
  provider: string
  providerPaymentId: string | null
  amount: number
  installmentCount: number
  status: PaymentStatus
  failureReason: string | null
  createdAt: string
  completedAt: string | null
}

export interface AdminShipment {
  sellerName: string
  carrier: string
  trackingNumber: string | null
  status: ShipmentStatus
  shippedAt: string | null
}

export interface AdminStatusEntry {
  from: OrderStatus | null
  to: OrderStatus
  note: string | null
  changedAt: string
}

export interface AdminOrderDetail {
  id: number
  orderNumber: string
  customerEmail: string
  customerName: string
  status: OrderStatus
  placedAt: string
  paidAt: string | null
  subtotal: number
  discountTotal: number
  shippingTotal: number
  grandTotal: number
  couponCode: string | null
  shippingCity: string | null
  items: AdminOrderItem[]
  payments: AdminPayment[]
  shipments: AdminShipment[]
  history: AdminStatusEntry[]
}

export type CouponState = 'running' | 'scheduled' | 'expired' | 'exhausted' | 'inactive'

export interface AdminCoupon {
  id: number
  code: string
  description: string
  type: CouponType
  value: number
  maxDiscountAmount: number | null
  minOrderAmount: number
  maxUses: number | null
  maxUsesPerUser: number | null
  usedCount: number
  sellerName: string | null
  startsAt: string
  endsAt: string
  isActive: boolean
  state: CouponState
}

export interface CreateCouponInput {
  code: string
  description: string
  type: CouponType
  value: number
  maxDiscountAmount: number | null
  minOrderAmount: number
  maxUses: number | null
  maxUsesPerUser: number | null
  sellerId: number | null
  startsOn: string
  endsOn: string
}

export interface CampaignHistoryEntry {
  at: string
  action: string
  couponCode: string | null
  actorEmail: string | null
  data: string | null
}

export interface SalesSummary {
  orderCount: number
  revenue: number
  averageOrderValue: number
  commission: number
  newCustomers: number
  /** Yüzde olarak (0-100). */
  paymentFailureRate: number
}

export interface DailySales {
  day: string
  orderCount: number
  revenue: number
}

export interface CategorySales {
  category: string
  quantity: number
  revenue: number
}

export interface SellerSales {
  sellerId: number
  storeName: string
  orderCount: number
  revenue: number
  commission: number
}

export interface SalesReport {
  from: string
  to: string
  summary: SalesSummary
  daily: DailySales[]
  topCategories: CategorySales[]
  topSellers: SellerSales[]
}
