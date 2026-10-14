import { useQuery } from '@tanstack/react-query'
import type { ReactNode } from 'react'
import { Link } from 'react-router'
import { sellerApi } from '../api/seller'
import { ProductStatus, type DailySalesPoint } from '../api/types'
import { ErrorState, LoadingBlock } from '../components/ErrorState'
import { formatPrice } from '../lib/format'
import { formatDay } from '../lib/status'

export function DashboardPage() {
  const dashboard = useQuery({ queryKey: ['seller', 'dashboard'], queryFn: ({ signal }) => sellerApi.dashboard(signal) })

  if (dashboard.isPending) return <LoadingBlock />
  if (dashboard.isError) return <ErrorState error={dashboard.error} onRetry={() => void dashboard.refetch()} />
  const d = dashboard.data

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-xl font-bold">Özet</h1>

      <section aria-labelledby="todo-title">
        <h2 id="todo-title" className="sr-only">Yapılacaklar ve ürünler</h2>
        <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
          <Stat label="Gönderilecek sipariş" value={String(d.ordersToShip)} to="/siparisler" highlight={d.ordersToShip > 0} />
          <Stat label="Düşük stoklu varyant" value={String(d.lowStockVariants)} to="/urunler" highlight={d.lowStockVariants > 0} />
          <Stat label="Aktif ürün" value={String(d.activeProducts)} to={`/urunler?status=${ProductStatus.Active}`} />
          <Stat label="Onay bekleyen ürün" value={String(d.pendingApprovalProducts)} to={`/urunler?status=${ProductStatus.PendingApproval}`} />
        </div>
      </section>

      <section aria-labelledby="sales-title">
        <h2 id="sales-title" className="mb-3 font-bold">Son 30 gün</h2>
        <div className="grid gap-3 sm:grid-cols-3">
          <Stat label="Satış" value={formatPrice(d.last30DaysSales)} />
          <Stat label="Komisyon" value={formatPrice(d.last30DaysCommission)} />
          <Stat label="Net kazanç" value={formatPrice(d.last30DaysNet)} />
        </div>
      </section>

      <SalesChart points={d.dailySales} />
    </div>
  )
}

function Stat({ label, value, to, highlight }: { label: string; value: ReactNode; to?: string; highlight?: boolean }) {
  const body = (
    <>
      <p className="text-sm text-muted">{label}</p>
      <p className={`mt-1 break-words text-2xl font-bold ${highlight ? 'text-accent-500' : ''}`}>{value}</p>
    </>
  )
  const base = 'min-w-0 rounded-xl border border-line bg-white p-4'
  return to ? <Link to={to} className={`${base} block hover:border-brand-600`}>{body}</Link> : <div className={base}>{body}</div>
}

function SalesChart({ points }: { points: DailySalesPoint[] }) {
  const max = Math.max(0, ...points.map((p) => p.sales))
  const total = points.reduce((sum, p) => sum + p.sales, 0)
  const summary = `Son ${points.length} günde toplam ${formatPrice(total)} satış. En yüksek günlük satış ${formatPrice(max)}.`

  return (
    <section aria-labelledby="chart-title" className="rounded-xl border border-line bg-white p-4 sm:p-5">
      <h2 id="chart-title" className="mb-4 font-bold">Günlük satış</h2>
      {points.length === 0 ? (
        <p className="text-sm text-muted">Bu dönemde satış verisi yok.</p>
      ) : (
        <>
          <div role="img" aria-label={summary}>
            <div className="flex h-40 items-end gap-px sm:gap-1" aria-hidden="true">
              {points.map((p) => (
                <div key={p.day} className="flex h-full min-w-0 flex-1 items-end" title={`${formatDay(p.day)}: ${formatPrice(p.sales)} (${p.orders} sipariş)`}>
                  <div
                    className="w-full rounded-t-sm bg-brand-600"
                    style={{ height: max > 0 ? `${Math.max((p.sales / max) * 100, p.sales > 0 ? 2 : 0)}%` : 0 }}
                  />
                </div>
              ))}
            </div>
            <div className="mt-2 flex justify-between text-xs text-muted" aria-hidden="true">
              <span>{formatDay(points[0].day)}</span>
              <span>{formatDay(points[points.length - 1].day)}</span>
            </div>
          </div>
          <details className="mt-4 text-sm">
            <summary className="cursor-pointer font-semibold text-brand-700">Tablo olarak göster</summary>
            <div className="mt-3 max-h-72 overflow-auto">
              <table className="w-full text-left">
                <caption className="sr-only">Günlük satış ve sipariş sayıları</caption>
                <thead className="sticky top-0 bg-white text-muted">
                  <tr>
                    <th scope="col" className="py-1.5 pr-3 font-medium">Gün</th>
                    <th scope="col" className="py-1.5 pr-3 text-right font-medium">Satış</th>
                    <th scope="col" className="py-1.5 text-right font-medium">Sipariş</th>
                  </tr>
                </thead>
                <tbody>
                  {points.map((p) => (
                    <tr key={p.day} className="border-t border-line">
                      <th scope="row" className="py-1.5 pr-3 font-normal">{formatDay(p.day)}</th>
                      <td className="py-1.5 pr-3 text-right">{formatPrice(p.sales)}</td>
                      <td className="py-1.5 text-right">{p.orders}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </details>
        </>
      )}
    </section>
  )
}
