import type { OrderStatus, ProductStatus } from '../api/types'
import { orderStatusLabel, orderStatusTone, productStatusLabel, productStatusTone } from '../lib/status'

const base = 'inline-block whitespace-nowrap rounded-full px-2.5 py-0.5 text-xs font-semibold'

export function ProductStatusBadge({ status }: { status: ProductStatus }) {
  return <span className={`${base} ${productStatusTone[status]}`}>{productStatusLabel[status]}</span>
}

export function OrderStatusBadge({ status }: { status: OrderStatus }) {
  return <span className={`${base} ${orderStatusTone[status]}`}>{orderStatusLabel[status]}</span>
}
