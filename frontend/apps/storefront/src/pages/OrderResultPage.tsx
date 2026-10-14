import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { Link, useParams } from 'react-router'
import { orderApi } from '../api/orders'
import { OrderStatus } from '../api/types'
import { ErrorState } from '../components/ErrorState'
import { formatPrice } from '../lib/format'
import { RetryPaymentButton } from './OrderDetailPage'

const POLL_MS = 1500
const MAX_WAIT_MS = 30_000

/**
 * Ödeme sayfasından dönüş. Sonuç webhook ile ayrıca geldiği için sipariş birkaç saniye
 * "ödeme bekleniyor" görünebilir; bu sürede durum yoklanır.
 */
export function OrderResultPage() {
  const { orderNumber = '' } = useParams()
  const [startedAt] = useState(() => Date.now())
  const order = useQuery({
    queryKey: ['orders', orderNumber],
    queryFn: ({ signal }) => orderApi.get(orderNumber, signal),
    refetchInterval: (query) => {
      const data = query.state.data
      if (!data || data.status !== OrderStatus.PendingPayment || data.lastPaymentFailed) return false
      return query.state.dataUpdatedAt - startedAt < MAX_WAIT_MS ? POLL_MS : false
    },
  })

  if (order.isPending) return <Waiting />
  if (order.isError) return <ErrorState error={order.error} onRetry={() => void order.refetch()} />

  const data = order.data
  const pending = data.status === OrderStatus.PendingPayment
  const timedOut = order.dataUpdatedAt - startedAt >= MAX_WAIT_MS

  if (pending && !data.lastPaymentFailed) {
    // Süre doldu diye "başarısız" deme: sonuç geç gelebilir, yeniden ödeme çift tahsilata yol açar.
    if (!timedOut) return <Waiting />
    return (
      <div className="mx-auto max-w-lg rounded-2xl border border-line bg-white p-8 text-center">
        <h1 className="text-xl font-bold">Ödeme sonucu henüz gelmedi</h1>
        <p className="mt-2 text-muted">Bankadan yanıt bekleniyor. Kartınızdan çekim yapıldıysa siparişiniz birkaç dakika içinde onaylanır; tekrar ödeme yapmayın.</p>
        <div className="mt-6 flex justify-center gap-3 text-sm">
          <button onClick={() => void order.refetch()} className="rounded-lg bg-brand-700 px-4 py-2 font-semibold text-white">Durumu yenile</button>
          <Link to={`/hesabim/siparisler/${data.orderNumber}`} className="rounded-lg border border-line px-4 py-2 font-semibold hover:bg-surface">Sipariş detayı</Link>
        </div>
      </div>
    )
  }

  const success = data.status !== OrderStatus.PendingPayment && data.status !== OrderStatus.Cancelled
  return (
    <div className="mx-auto max-w-lg rounded-2xl border border-line bg-white p-8 text-center">
      {success ? (
        <>
          <p className="mx-auto mb-4 flex h-14 w-14 items-center justify-center rounded-full bg-brand-50 text-2xl text-brand-700" aria-hidden>✓</p>
          <h1 className="text-2xl font-bold">Siparişiniz alındı</h1>
          <p className="mt-2 text-muted">
            <strong className="text-ink">{data.orderNumber}</strong> numaralı siparişinizin ödemesi onaylandı. Toplam {formatPrice(data.grandTotal)}.
          </p>
        </>
      ) : data.status === OrderStatus.Cancelled ? (
        <>
          <h1 className="text-2xl font-bold">Sipariş iptal edildi</h1>
          <p className="mt-2 text-muted">Ödeme süresi dolduğu için {data.orderNumber} iptal edildi. Ürünler hâlâ stoktaysa yeniden sipariş verebilirsiniz.</p>
        </>
      ) : (
        <>
          <h1 className="text-2xl font-bold">Ödeme tamamlanamadı</h1>
          <p className="mt-2 text-muted">Siparişiniz kaydedildi ve ürünler sizin için ayrıldı. Ödemeyi tekrar deneyebilirsiniz.</p>
          <div className="mt-5"><RetryPaymentButton orderNumber={data.orderNumber} /></div>
        </>
      )}
      <div className="mt-6 flex justify-center gap-3 text-sm">
        <Link to={`/hesabim/siparisler/${data.orderNumber}`} className="rounded-lg border border-line px-4 py-2 font-semibold hover:bg-surface">Sipariş detayı</Link>
        <Link to="/" className="rounded-lg px-4 py-2 text-muted hover:text-ink">Alışverişe devam et</Link>
      </div>
    </div>
  )
}

function Waiting() {
  return (
    <div className="mx-auto max-w-lg rounded-2xl border border-line bg-white p-8 text-center" aria-busy="true" aria-live="polite">
      <div className="mx-auto mb-4 h-10 w-10 animate-spin rounded-full border-4 border-line border-t-brand-700" />
      <h1 className="text-xl font-bold">Ödemeniz kontrol ediliyor</h1>
      <p className="mt-2 text-muted">Bu birkaç saniye sürebilir, sayfayı kapatmayın.</p>
    </div>
  )
}
