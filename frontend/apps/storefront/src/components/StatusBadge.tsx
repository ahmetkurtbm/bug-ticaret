import type { OrderStatus } from '../api/types'
import { orderStatusLabel, orderStatusTone } from '../lib/orderStatus'

export function StatusBadge({ status }: { status: OrderStatus }) {
  return <span className={`inline-block rounded-full px-2.5 py-0.5 text-xs font-semibold ${orderStatusTone[status]}`}>{orderStatusLabel[status]}</span>
}
