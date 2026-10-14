import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { useSearchParams } from 'react-router'
import { ApiError } from '../api/client'
import { sellerApi } from '../api/seller'
import { OrderStatus, type OrderFilter, type SellerOrder } from '../api/types'
import { ErrorState, LoadingBlock } from '../components/ErrorState'
import { Alert, Field, primaryButton, SelectField } from '../components/Field'
import { Pagination } from '../components/Pagination'
import { OrderStatusBadge } from '../components/StatusBadge'
import { formatPrice } from '../lib/format'
import { formatDate } from '../lib/status'

const PAGE_SIZE = 20
const CARRIERS = ['Yurtiçi Kargo', 'Aras Kargo', 'MNG Kargo', 'PTT Kargo', 'Sürat Kargo', 'HepsiJet'] as const
const TABS: { filter: OrderFilter; label: string }[] = [
  { filter: 'to_ship', label: 'Gönderilecek' },
  { filter: 'shipped', label: 'Gönderilenler' },
  { filter: 'all', label: 'Tümü' },
]

export function OrdersPage() {
  const [params, setParams] = useSearchParams()
  const filter = TABS.find((t) => t.filter === params.get('filter'))?.filter ?? 'to_ship'
  const page = Math.max(1, Number(params.get('page')) || 1)

  const orders = useQuery({
    queryKey: ['seller', 'orders', { filter, page }],
    queryFn: ({ signal }) => sellerApi.orders({ filter, page, pageSize: PAGE_SIZE }, signal),
    placeholderData: keepPreviousData,
  })

  function go(nextFilter: OrderFilter, nextPage: number) {
    const next = new URLSearchParams()
    if (nextFilter !== 'to_ship') next.set('filter', nextFilter)
    if (nextPage > 1) next.set('page', String(nextPage))
    setParams(next)
  }

  return (
    <div className="flex flex-col gap-4">
      <h1 className="text-xl font-bold">Siparişler</h1>

      <div role="tablist" aria-label="Sipariş filtresi" className="flex gap-1 overflow-x-auto border-b border-line">
        {TABS.map((tab) => (
          <button
            key={tab.filter}
            role="tab"
            id={`tab-${tab.filter}`}
            aria-selected={tab.filter === filter}
            aria-controls="orders-panel"
            onClick={() => go(tab.filter, 1)}
            className={`whitespace-nowrap border-b-2 px-4 py-2.5 text-sm font-medium ${tab.filter === filter ? 'border-brand-700 text-brand-700' : 'border-transparent text-muted hover:text-ink'}`}
          >
            {tab.label}
          </button>
        ))}
      </div>

      <div id="orders-panel" role="tabpanel" aria-labelledby={`tab-${filter}`}>
        {orders.isPending ? (
          <LoadingBlock />
        ) : orders.isError ? (
          <ErrorState error={orders.error} onRetry={() => void orders.refetch()} />
        ) : orders.data.items.length === 0 ? (
          <div className="rounded-xl border border-line bg-white px-6 py-14 text-center">
            <p className="font-semibold">{filter === 'to_ship' ? 'Gönderilecek sipariş yok' : 'Sipariş bulunamadı'}</p>
          </div>
        ) : (
          <div aria-busy={orders.isFetching}>
            <ul className="flex flex-col gap-4">
              {orders.data.items.map((order) => (
                <li key={order.orderNumber}>
                  <OrderCard order={order} />
                </li>
              ))}
            </ul>
            <Pagination page={orders.data.page} totalPages={orders.data.totalPages} onChange={(p) => go(filter, p)} />
          </div>
        )}
      </div>
    </div>
  )
}

function OrderCard({ order }: { order: SellerOrder }) {
  const canShip = !order.shipment && (order.status === OrderStatus.Paid || order.status === OrderStatus.Preparing)
  const address = [order.addressLine, [order.district, order.city].filter(Boolean).join(' / ')].filter(Boolean).join(', ')

  return (
    <article aria-label={`Sipariş ${order.orderNumber}`} className="rounded-xl border border-line bg-white p-4 sm:p-5">
      <header className="flex flex-wrap items-center justify-between gap-2">
        <div className="min-w-0">
          <h2 className="break-all font-bold">{order.orderNumber}</h2>
          <p className="text-sm text-muted">{formatDate(order.placedAt)}</p>
        </div>
        <OrderStatusBadge status={order.status} />
      </header>

      <div className="mt-3 text-sm">
        <p className="font-semibold">{order.customerName ?? 'Alıcı bilgisi yok'}</p>
        {address && <p className="break-words text-muted">{address}</p>}
      </div>

      <ul className="mt-3 divide-y divide-line border-y border-line text-sm">
        {order.items.map((item) => (
          <li key={`${item.sku}-${item.productName}`} className="flex justify-between gap-3 py-2">
            <div className="min-w-0">
              <p className="break-words">{item.quantity} × {item.productName}</p>
              <p className="break-all text-xs text-muted">{item.sku}{item.variantLabel ? ` · ${item.variantLabel}` : ''}</p>
            </div>
            <span className="shrink-0">{formatPrice(item.lineTotal)}</span>
          </li>
        ))}
      </ul>

      <p className="mt-3 flex justify-between font-bold">
        <span>Satıcı toplamı</span>
        <span>{formatPrice(order.sellerTotal)}</span>
      </p>

      {order.shipment && (
        <p className="mt-3 rounded-lg bg-surface px-3 py-2 text-sm">
          <span className="font-semibold">{order.shipment.carrier}</span>
          {order.shipment.trackingNumber && <> · Takip no: <span className="break-all font-mono">{order.shipment.trackingNumber}</span></>}
          {order.shipment.shippedAt && <span className="text-muted"> · {formatDate(order.shipment.shippedAt)}</span>}
        </p>
      )}

      {canShip && <ShipForm orderNumber={order.orderNumber} />}
    </article>
  )
}

function ShipForm({ orderNumber }: { orderNumber: string }) {
  const queryClient = useQueryClient()
  const [carrier, setCarrier] = useState('')
  const [trackingNumber, setTrackingNumber] = useState('')
  const ship = useMutation({
    mutationFn: () => sellerApi.ship(orderNumber, { carrier, trackingNumber }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['seller', 'orders'] })
      void queryClient.invalidateQueries({ queryKey: ['seller', 'dashboard'] })
    },
  })
  const errors = ship.error instanceof ApiError ? ship.error.fieldErrors : {}
  const general = ship.error && Object.keys(errors).length === 0 ? (ship.error instanceof ApiError ? ship.error.message : 'Kargoya verilemedi.') : null

  function onSubmit(e: FormEvent) {
    e.preventDefault()
    if (!ship.isPending) ship.mutate()
  }

  return (
    <form onSubmit={onSubmit} noValidate aria-busy={ship.isPending} className="mt-4 flex flex-col gap-3 border-t border-line pt-4">
      <h3 className="font-semibold">Kargoya ver</h3>
      {general && <Alert>{general}</Alert>}
      <div className="grid gap-3 sm:grid-cols-2">
        <SelectField id={`carrier-${orderNumber}`} label="Kargo firması" name={`carrier-${orderNumber}`} value={carrier} onChange={(e) => setCarrier(e.target.value)} error={errors.carrier}>
          <option value="">Seçin</option>
          {CARRIERS.map((c) => (
            <option key={c} value={c}>{c}</option>
          ))}
        </SelectField>
        <Field
          id={`tracking-${orderNumber}`}
          label="Takip numarası"
          name={`tracking-${orderNumber}`}
          value={trackingNumber}
          onChange={(e) => setTrackingNumber(e.target.value.toUpperCase().replace(/\s/g, ''))}
          error={errors.trackingNumber}
          hint="8-20 karakter, büyük harf ve rakam"
          maxLength={20}
          autoCapitalize="characters"
          autoComplete="off"
        />
      </div>
      <div>
        <button disabled={ship.isPending} className={primaryButton}>{ship.isPending ? 'Kaydediliyor…' : 'Kargoya ver'}</button>
      </div>
    </form>
  )
}
