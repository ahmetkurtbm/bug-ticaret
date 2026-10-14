// Ortak DTO'lar (Tezgah.Application). Alan adları camelCase. Uygulamaya özel tipler aşağıya eklenir.

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
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

/** ASP.NET Core ProblemDetails / ValidationProblemDetails. */
export interface ProblemDetails {
  status?: number
  title?: string
  detail?: string
  code?: string
  errors?: Record<string, string[]>
}

// ---- Satıcı paneli (Tezgah.Application.Sellers). Enum'lar sayı olarak gelir.
export const ProductStatus = { Draft: 1, PendingApproval: 2, Active: 3, Rejected: 4, Archived: 5 } as const
export type ProductStatus = (typeof ProductStatus)[keyof typeof ProductStatus]

export const SellerStatus = { Pending: 1, Active: 2, Suspended: 3, Closed: 4 } as const
export type SellerStatus = (typeof SellerStatus)[keyof typeof SellerStatus]

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

export interface CategoryNode {
  id: number
  name: string
  slug: string
  children: CategoryNode[]
}

export interface SellerApplicationInput {
  storeName: string
  legalName: string
  taxNumber: string
  taxOffice: string
  iban: string
  city: string
}

export interface SellerProfile {
  id: number
  storeName: string
  slug: string
  legalName: string
  taxNumber: string
  taxOffice: string
  iban: string | null
  city: string
  status: SellerStatus
  commissionRate: number
  rating: number
  ratingCount: number
  createdAt: string
  approvedAt: string | null
}

export interface DailySalesPoint {
  day: string
  sales: number
  orders: number
}

export interface SellerDashboard {
  ordersToShip: number
  lowStockVariants: number
  activeProducts: number
  pendingApprovalProducts: number
  last30DaysSales: number
  last30DaysCommission: number
  last30DaysNet: number
  dailySales: DailySalesPoint[]
}

export interface SellerProductListItem {
  id: number
  name: string
  slug: string
  imageUrl: string | null
  categoryName: string
  status: ProductStatus
  variantCount: number
  totalStock: number
  minPrice: number
  soldCount: number
  createdAt: string
}

export interface SellerVariant {
  id: number
  sku: string
  barcode: string | null
  color: string | null
  size: string | null
  price: number
  listPrice: number | null
  vatRate: number
  isActive: boolean
  quantity: number
  reserved: number
}

export interface SellerProductDetail {
  id: number
  name: string
  slug: string
  description: string | null
  categoryId: number
  categoryName: string
  brandId: number | null
  brandName: string | null
  status: ProductStatus
  images: string[]
  variants: SellerVariant[]
}

export interface CreateVariantInput {
  sku: string
  barcode: string | null
  color: string | null
  size: string | null
  price: number
  listPrice: number | null
  vatRate: number
  stock: number
}

export interface CreateProductInput {
  name: string
  description: string | null
  categoryId: number
  brandId: number | null
  imageUrls: string[]
  variants: CreateVariantInput[]
  submitForApproval: boolean
}

export interface UpdateProductInput {
  name: string
  description: string | null
  categoryId: number
  brandId: number | null
}

export interface SellerOrderItem {
  productName: string
  sku: string
  variantLabel: string | null
  quantity: number
  unitPrice: number
  lineTotal: number
}

export interface SellerShipment {
  carrier: string
  trackingNumber: string | null
  shippedAt: string | null
}

export interface SellerOrder {
  orderNumber: string
  placedAt: string
  status: OrderStatus
  customerName: string | null
  city: string | null
  district: string | null
  addressLine: string | null
  items: SellerOrderItem[]
  sellerTotal: number
  shipment: SellerShipment | null
}

export type OrderFilter = 'to_ship' | 'shipped' | 'all'
