import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { Link, useSearchParams } from 'react-router'
import { orderApi } from '../api/orders'
import { ErrorState } from '../components/ErrorState'
import { Pagination } from '../components/Pagination'
import { StatusBadge } from '../components/StatusBadge'
import { formatPrice } from '../lib/format'
import { formatDate } from '../lib/orderStatus'

export function OrdersPage() {
  const [params, setParams] = useSearchParams()
  const page = Math.max(1, Number(params.get('sayfa')) || 1)
  const orders = useQuery({
    queryKey: ['orders', 'list', page],
    queryFn: ({ signal }) => orderApi.list(page, signal),
    placeholderData: keepPreviousData,
  })

  if (orders.isPending) return <div className="h-72 animate-pulse rounded-xl bg-line/60" aria-busy="true" />
  if (orders.isError) return <ErrorState error={orders.error} onRetry={() => void orders.refetch()} />

  const data = orders.data
  return (
    <section aria-labelledby="orders-title">
      <h1 id="orders-title" className="mb-4 text-xl font-bold">Siparişlerim</h1>
      {data.items.length === 0 ? (
        <div className="rounded-xl border border-line bg-white px-6 py-16 text-center">
          <p className="font-semibold">Henüz siparişiniz yok.</p>
          <Link to="/" className="mt-4 inline-block text-sm font-semibold text-brand-700 underline">Alışverişe başla</Link>
        </div>
      ) : (
        <ul className="flex flex-col gap-3">
          {data.items.map((o) => (
            <li key={o.orderNumber}>
              <Link to={`/hesabim/siparisler/${o.orderNumber}`} className="flex flex-wrap items-center gap-4 rounded-xl border border-line bg-white p-4 hover:border-brand-700">
                <div className="flex -space-x-3">
                  {o.imageUrls.filter(Boolean).slice(0, 3).map((url, i) => (
                    <img key={i} src={url} alt="" className="h-14 w-14 rounded-lg border-2 border-white bg-surface object-cover" loading="lazy" />
                  ))}
                </div>
                <div className="flex min-w-40 flex-1 flex-col gap-0.5">
                  <span className="font-semibold">{o.orderNumber}</span>
                  <span className="text-sm text-muted">{formatDate(o.placedAt)} · {o.itemCount} ürün</span>
                </div>
                <StatusBadge status={o.status} />
                <span className="w-28 text-right font-bold">{formatPrice(o.grandTotal)}</span>
              </Link>
            </li>
          ))}
        </ul>
      )}
      {data.totalPages > 1 && <div className="mt-6"><Pagination page={data.page} totalPages={data.totalPages} onChange={(p) => setParams({ sayfa: String(p) })} /></div>}
    </section>
  )
}
