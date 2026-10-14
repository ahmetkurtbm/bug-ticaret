import { useMutation, useQuery } from '@tanstack/react-query'
import { Link, useLocation, useParams } from 'react-router'
import { ApiError } from '../api/client'
import { orderApi } from '../api/orders'
import { ErrorState } from '../components/ErrorState'
import { StatusBadge } from '../components/StatusBadge'
import { formatPrice } from '../lib/format'
import { formatDate, orderStatusLabel } from '../lib/orderStatus'

export function OrderDetailPage() {
  const { orderNumber = '' } = useParams()
  const location = useLocation()
  const paymentError = (location.state as { paymentError?: string | null } | null)?.paymentError
  const order = useQuery({ queryKey: ['orders', orderNumber], queryFn: ({ signal }) => orderApi.get(orderNumber, signal) })

  if (order.isPending) return <div className="h-72 animate-pulse rounded-xl bg-line/60" aria-busy="true" />
  if (order.isError) return <ErrorState error={order.error} onRetry={() => void order.refetch()} />

  const o = order.data
  return (
    <div className="flex flex-col gap-6">
      <Link to="/hesabim/siparisler" className="text-sm text-muted hover:text-ink">← Siparişlerim</Link>
      <header className="flex flex-wrap items-center gap-3">
        <h1 className="text-xl font-bold">{o.orderNumber}</h1>
        <StatusBadge status={o.status} />
        <span className="text-sm text-muted">{formatDate(o.placedAt)}</span>
      </header>

      {paymentError && o.canRetryPayment && (
        <p className="rounded-lg bg-amber-50 px-4 py-3 text-sm text-amber-900" role="alert">{paymentError}</p>
      )}
      {o.canRetryPayment && (
        <div className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-amber-300 bg-amber-50 p-4">
          <p className="text-sm text-amber-900">Bu siparişin ödemesi henüz alınmadı. Ödeme yapılmazsa sipariş 30 dakika içinde iptal edilir.</p>
          <RetryPaymentButton orderNumber={o.orderNumber} />
        </div>
      )}

      <div className="grid gap-6 lg:grid-cols-[1fr_20rem]">
        <section aria-labelledby="items-title" className="rounded-xl border border-line bg-white">
          <h2 id="items-title" className="border-b border-line px-5 py-3 font-bold">Ürünler</h2>
          <ul className="divide-y divide-line">
            {o.items.map((i) => (
              <li key={i.variantId} className="flex gap-4 p-4">
                <div className="h-20 w-20 shrink-0 overflow-hidden rounded-lg bg-surface">
                  {i.imageUrl && <img src={i.imageUrl} alt="" className="h-full w-full object-cover" loading="lazy" />}
                </div>
                <div className="flex flex-1 flex-col gap-1">
                  {i.productSlug ? (
                    <Link to={`/urun/${i.productSlug}`} className="font-medium hover:text-brand-700">{i.productName}</Link>
                  ) : (
                    <span className="font-medium">{i.productName}</span>
                  )}
                  {i.variantLabel && <span className="text-sm text-muted">{i.variantLabel}</span>}
                  <span className="text-sm text-muted">{i.quantity} × {formatPrice(i.unitPrice)}</span>
                </div>
                <div className="text-right">
                  <p className="font-bold">{formatPrice(i.lineTotal)}</p>
                  {i.discountAmount > 0 && <p className="text-xs text-brand-700">-{formatPrice(i.discountAmount)} indirim</p>}
                </div>
              </li>
            ))}
          </ul>
        </section>

        <aside className="flex flex-col gap-4 self-start">
          <section className="rounded-xl border border-line bg-white p-5 text-sm">
            <h2 className="mb-3 font-bold">Ödeme özeti</h2>
            <dl className="flex flex-col gap-2">
              <Row label="Ürünler" value={formatPrice(o.subtotal)} />
              {o.discountTotal > 0 && <Row label={`İndirim${o.couponCode ? ` (${o.couponCode})` : ''}`} value={`-${formatPrice(o.discountTotal)}`} />}
              <Row label="Kargo" value={o.shippingTotal === 0 ? 'Bedava' : formatPrice(o.shippingTotal)} />
              <div className="mt-1 flex justify-between border-t border-line pt-2 text-base font-bold">
                <dt>Toplam</dt>
                <dd>{formatPrice(o.grandTotal)}</dd>
              </div>
            </dl>
          </section>
          <section className="rounded-xl border border-line bg-white p-5 text-sm">
            <h2 className="mb-2 font-bold">Teslimat adresi</h2>
            {o.shippingAddress ? (
              <address className="not-italic text-muted">
                <span className="block text-ink">{o.shippingAddress.fullName}</span>
                {o.shippingAddress.line}
                <br />
                {o.shippingAddress.district} / {o.shippingAddress.city} {o.shippingAddress.postalCode}
              </address>
            ) : (
              <p className="text-muted">Adres bilgisi yok.</p>
            )}
          </section>
          <section className="rounded-xl border border-line bg-white p-5 text-sm">
            <h2 className="mb-2 font-bold">Sipariş geçmişi</h2>
            <ol className="flex flex-col gap-2">
              {o.history.map((h, idx) => (
                <li key={idx} className="flex justify-between gap-3">
                  <span>{orderStatusLabel[h.status]}</span>
                  <span className="text-muted">{formatDate(h.changedAt)}</span>
                </li>
              ))}
            </ol>
          </section>
        </aside>
      </div>
    </div>
  )
}

export function RetryPaymentButton({ orderNumber }: { orderNumber: string }) {
  const retry = useMutation({
    mutationFn: () => orderApi.retryPayment(orderNumber),
    onSuccess: (r) => {
      if (r.checkoutUrl) window.location.assign(r.checkoutUrl)
    },
  })
  const message = retry.error instanceof ApiError ? retry.error.message : retry.data?.paymentError ?? (retry.isError ? 'Ödeme başlatılamadı.' : null)
  return (
    <div className="flex flex-col items-center gap-2">
      <button
        onClick={() => retry.mutate()}
        disabled={retry.isPending || Boolean(retry.data?.checkoutUrl)}
        className="rounded-lg bg-brand-700 px-5 py-2.5 text-sm font-semibold text-white disabled:opacity-60"
      >
        {retry.isPending ? 'Yönlendiriliyor…' : 'Ödemeyi tamamla'}
      </button>
      {message && <p className="text-xs text-red-700" role="alert">{message}</p>}
    </div>
  )
}

function Row({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex justify-between">
      <dt className="text-muted">{label}</dt>
      <dd>{value}</dd>
    </div>
  )
}
