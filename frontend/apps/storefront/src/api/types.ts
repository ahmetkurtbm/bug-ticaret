// Backend DTO'larının karşılıkları (Tezgah.Application). Alan adları camelCase.

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface CategoryNode {
  id: number
  name: string
  slug: string
  children: CategoryNode[]
}

export interface ProductCard {
  id: number
  name: string
  slug: string
  brandName: string | null
  sellerName: string
  price: number
  listPrice: number | null
  imageUrl: string | null
  rating: number
  reviewCount: number
}

export type ProductSort = 'popular' | 'price_asc' | 'price_desc' | 'newest' | 'rating'

export interface ProductSearchParams {
  category?: string
  brand?: string
  q?: string
  minPrice?: number
  maxPrice?: number
  sort?: ProductSort
  page?: number
  pageSize?: number
}

export interface Variant {
  id: number
  sku: string
  color: string | null
  size: string | null
  price: number
  listPrice: number | null
  availableQuantity: number
}

export interface ProductDetail {
  id: number
  name: string
  slug: string
  description: string | null
  brandName: string | null
  seller: { id: number; storeName: string; slug: string; rating: number; ratingCount: number }
  breadcrumbs: { name: string; slug: string }[]
  images: string[]
  variants: Variant[]
  attributes: { name: string; value: string; unit: string | null }[]
  rating: number
  reviewCount: number
}

export interface CartLine {
  id: number
  variantId: number
  productId: number
  productName: string
  productSlug: string
  variantLabel: string | null
  imageUrl: string | null
  sellerName: string
  unitPrice: number
  priceChanged: boolean
  quantity: number
  availableQuantity: number
  discountAmount: number
  lineTotal: number
}

export interface Cart {
  lines: CartLine[]
  itemCount: number
  subtotal: number
  discountTotal: number
  shippingTotal: number
  vatTotal: number
  grandTotal: number
  couponCode: string | null
  couponError: string | null
  freeShippingRemaining: number
}

export interface AuthUser {
  id: number
  email: string
  firstName: string
  lastName: string
  roles: string[]
  sellerId: number | null
}

export interface AuthResponse {
  accessToken: string
  accessTokenExpiresAt: string
  refreshToken: string
  user: AuthUser
}

export interface Address {
  id: number
  title: string
  fullName: string
  phone: string | null
  city: string
  district: string
  line: string
  postalCode: string | null
  isDefault: boolean
}

export type NewAddress = Omit<Address, 'id' | 'phone'> & { phone: string }

/** Tezgah.Domain.Orders.OrderStatus ile aynı sayılar. */
export const OrderStatus = {
  PendingPayment: 1,
  Paid: 2,
  Preparing: 3,
  Shipped: 4,
  Delivered: 5,
  Cancelled: 6,
  Returned: 7,
} as const
export type OrderStatus = (typeof OrderStatus)[keyof typeof OrderStatus]

export interface PlaceOrderResponse {
  orderNumber: string
  checkoutUrl: string | null
  paymentError: string | null
}

export interface OrderSummary {
  orderNumber: string
  status: OrderStatus
  placedAt: string
  grandTotal: number
  itemCount: number
  imageUrls: string[]
}

export interface OrderItem {
  variantId: number
  productName: string
  productSlug: string | null
  variantLabel: string | null
  imageUrl: string | null
  quantity: number
  unitPrice: number
  discountAmount: number
  lineTotal: number
}

export interface OrderDetail {
  orderNumber: string
  status: OrderStatus
  placedAt: string
  paidAt: string | null
  subtotal: number
  discountTotal: number
  shippingTotal: number
  vatTotal: number
  grandTotal: number
  couponCode: string | null
  shippingAddress: { fullName: string; phone: string | null; city: string; district: string; line: string; postalCode: string | null } | null
  items: OrderItem[]
  history: { status: OrderStatus; changedAt: string }[]
  canRetryPayment: boolean
  lastPaymentFailed: boolean
}

/** ASP.NET Core ProblemDetails / ValidationProblemDetails. */
export interface ProblemDetails {
  status?: number
  title?: string
  detail?: string
  code?: string
  errors?: Record<string, string[]>
}
