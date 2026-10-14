import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { useSearchParams } from 'react-router'
import { adminApi } from '../api/admin'
import type { DailySales } from '../api/adminTypes'
import { ErrorState, LoadingBlock } from '../components/ErrorState'
import { Field } from '../components/Field'
import { rowClass, RowTh, TableShell, tdClass, Th } from '../components/Table'
import { addDays, daysBetween, formatPlainDay, formatShortDay, isDayString, startOfMonth, todayInIstanbul } from '../lib/dates'
import { formatPrice } from '../lib/format'

const MAX_RANGE_DAYS = 366

export function DashboardPage() {
  const [params, setParams] = useSearchParams()
  const today = todayInIstanbul()
  const rawTo = params.get('to')
  const rawFrom = params.get('from')
  const to = isDayString(rawTo) ? rawTo : today
  const from = isDayString(rawFrom) ? rawFrom : addDays(to, -29)

  const rangeError =
    from > to ? 'Başlangıç tarihi bitiş tarihinden sonra olamaz.' : daysBetween(from, to) > MAX_RANGE_DAYS ? 'Tarih aralığı en fazla 1 yıl olabilir.' : null

  const report = useQuery({
    queryKey: ['admin', 'report', 'sales', from, to],
    queryFn: ({ signal }) => adminApi.salesReport(from, to, signal),
    enabled: !rangeError,
    placeholderData: keepPreviousData,
  })

  const setRange = (nextFrom: string, nextTo: string) => setParams({ from: nextFrom, to: nextTo }, { replace: true })
  const presets = [
    { label: 'Son 7 gün', from: addDays(today, -6), to: today },
    { label: 'Son 30 gün', from: addDays(today, -29), to: today },
    { label: 'Bu ay', from: startOfMonth(today), to: today },
  ]

  return (
    <section aria-labelledby="dash-title" className="flex flex-col gap-6">
      <h1 id="dash-title" className="text-xl font-bold">Gösterge</h1>

      <div className="flex flex-wrap items-end gap-4 rounded-xl border border-line bg-white p-4">
        <div className="grid grid-cols-1 gap-3 min-[420px]:grid-cols-2">
          <Field label="Başlangıç" name="from" type="date" value={from} max={to} onChange={(e) => e.target.value && setRange(e.target.value, to)} />
          <Field label="Bitiş" name="to" type="date" value={to} onChange={(e) => e.target.value && setRange(from, e.target.value)} />
        </div>
        <div className="flex flex-wrap gap-2" role="group" aria-label="Hazır aralıklar">
          {presets.map((p) => {
            const active = p.from === from && p.to === to
            return (
              <button
                key={p.label}
                type="button"
                aria-pressed={active}
                onClick={() => setRange(p.from, p.to)}
                className={`rounded-lg border px-3 py-2 text-sm ${active ? 'border-brand-700 bg-brand-50 font-semibold text-brand-800' : 'border-line hover:bg-surface'}`}
              >
                {p.label}
              </button>
            )
          })}
        </div>
      </div>

      {rangeError ? (
        <p role="alert" className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{rangeError}</p>
      ) : report.isPending ? (
        <LoadingBlock className="h-96" />
      ) : report.isError ? (
        <ErrorState error={report.error} onRetry={() => void report.refetch()} />
      ) : (
        <div className="flex flex-col gap-6" aria-busy={report.isFetching}>
          <p className="text-sm text-muted">{formatPlainDay(report.data.from)} – {formatPlainDay(report.data.to)} arası</p>
          <SummaryCards summary={report.data.summary} />
          <RevenueChart daily={report.data.daily} />
          <div className="grid gap-6 xl:grid-cols-2">
            <section aria-labelledby="cat-title" className="min-w-0">
              <h2 id="cat-title" className="mb-2 font-semibold">En çok satan kategoriler</h2>
              {report.data.topCategories.length === 0 ? (
                <Empty />
              ) : (
                <TableShell caption="En çok satan kategoriler">
                  <thead>
                    <tr><Th>Kategori</Th><Th className="text-right">Adet</Th><Th className="text-right">Ciro</Th></tr>
                  </thead>
                  <tbody>
                    {report.data.topCategories.map((c) => (
                      <tr key={c.category} className={rowClass}>
                        <RowTh>{c.category}</RowTh>
                        <td className={`${tdClass} text-right`}>{c.quantity}</td>
                        <td className={`${tdClass} text-right`}>{formatPrice(c.revenue)}</td>
                      </tr>
                    ))}
                  </tbody>
                </TableShell>
              )}
            </section>
            <section aria-labelledby="seller-title" className="min-w-0">
              <h2 id="seller-title" className="mb-2 font-semibold">En çok satan satıcılar</h2>
              {report.data.topSellers.length === 0 ? (
                <Empty />
              ) : (
                <TableShell caption="En çok satan satıcılar">
                  <thead>
                    <tr><Th>Mağaza</Th><Th className="text-right">Sipariş</Th><Th className="text-right">Ciro</Th><Th className="text-right">Komisyon</Th></tr>
                  </thead>
                  <tbody>
                    {report.data.topSellers.map((s) => (
                      <tr key={s.sellerId} className={rowClass}>
                        <RowTh>{s.storeName}</RowTh>
                        <td className={`${tdClass} text-right`}>{s.orderCount}</td>
                        <td className={`${tdClass} text-right`}>{formatPrice(s.revenue)}</td>
                        <td className={`${tdClass} text-right`}>{formatPrice(s.commission)}</td>
                      </tr>
                    ))}
                  </tbody>
                </TableShell>
              )}
            </section>
          </div>
        </div>
      )}
    </section>
  )
}

const Empty = () => <p className="rounded-xl border border-line bg-white px-4 py-8 text-center text-sm text-muted">Bu aralıkta veri yok.</p>

function SummaryCards({ summary }: { summary: { orderCount: number; revenue: number; averageOrderValue: number; commission: number; newCustomers: number; paymentFailureRate: number } }) {
  const cards = [
    { label: 'Sipariş', value: summary.orderCount.toLocaleString('tr-TR') },
    { label: 'Ciro', value: formatPrice(summary.revenue) },
    { label: 'Ortalama sepet', value: formatPrice(summary.averageOrderValue) },
    { label: 'Komisyon', value: formatPrice(summary.commission) },
    { label: 'Yeni müşteri', value: summary.newCustomers.toLocaleString('tr-TR') },
    { label: 'Ödeme hata oranı', value: `%${summary.paymentFailureRate.toLocaleString('tr-TR', { maximumFractionDigits: 2 })}` },
  ]
  return (
    <dl className="grid grid-cols-1 gap-3 min-[420px]:grid-cols-2 lg:grid-cols-3">
      {cards.map((c) => (
        <div key={c.label} className="min-w-0 rounded-xl border border-line bg-white p-4">
          <dt className="text-sm text-muted">{c.label}</dt>
          <dd className="mt-1 truncate text-xl font-bold">{c.value}</dd>
        </div>
      ))}
    </dl>
  )
}

function RevenueChart({ daily }: { daily: DailySales[] }) {
  const max = Math.max(0, ...daily.map((d) => d.revenue))
  const total = daily.reduce((sum, d) => sum + d.revenue, 0)
  return (
    <section aria-labelledby="chart-title" className="rounded-xl border border-line bg-white p-4">
      <h2 id="chart-title" className="mb-3 font-semibold">Günlük ciro</h2>
      {daily.length === 0 ? (
        <Empty />
      ) : (
        <>
          <div
            role="img"
            aria-label={`${daily.length} günlük ciro grafiği. Toplam ${formatPrice(total)}, en yüksek gün ${formatPrice(max)}. Ayrıntılar aşağıdaki tabloda.`}
            className="flex h-44 items-end gap-px border-b border-line"
          >
            {daily.map((d) => (
              <div key={d.day} className="flex h-full min-w-0 flex-1 items-end" title={`${formatPlainDay(d.day)}: ${formatPrice(d.revenue)} · ${d.orderCount} sipariş`}>
                <div className="w-full rounded-t-sm bg-brand-600" style={{ height: max > 0 ? `${Math.max((d.revenue / max) * 100, d.revenue > 0 ? 2 : 0)}%` : 0 }} />
              </div>
            ))}
          </div>
          <div className="mt-1 flex justify-between text-xs text-muted" aria-hidden="true">
            <span>{formatShortDay(daily[0].day)}</span>
            <span>En yüksek: {formatPrice(max)}</span>
            <span>{formatShortDay(daily[daily.length - 1].day)}</span>
          </div>
          <details className="mt-4">
            <summary className="cursor-pointer text-sm font-semibold text-brand-700">Tablo olarak göster</summary>
            <div className="mt-2 max-h-80 overflow-auto rounded-lg border border-line">
              <table className="w-full text-left text-sm">
                <caption className="sr-only">Günlük ciro</caption>
                <thead>
                  <tr><Th>Gün</Th><Th className="text-right">Sipariş</Th><Th className="text-right">Ciro</Th></tr>
                </thead>
                <tbody>
                  {daily.map((d) => (
                    <tr key={d.day} className={rowClass}>
                      <RowTh className="font-medium">{formatPlainDay(d.day)}</RowTh>
                      <td className={`${tdClass} text-right`}>{d.orderCount}</td>
                      <td className={`${tdClass} text-right`}>{formatPrice(d.revenue)}</td>
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
