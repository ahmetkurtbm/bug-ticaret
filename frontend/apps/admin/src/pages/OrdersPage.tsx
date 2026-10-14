import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { Link, useNavigate, useSearchParams } from 'react-router'
import { adminApi } from '../api/admin'
import { OrderStatus } from '../api/adminTypes'
import { ErrorState, LoadingBlock } from '../components/ErrorState'
import { Field, SelectField } from '../components/Field'
import { Pagination } from '../components/Pagination'
import { MetaBadge } from '../components/StatusBadge'
import { rowClass, TableShell, tdClass, Th } from '../components/Table'
import { formatDateTime, isDayString } from '../lib/dates'
import { formatPrice } from '../lib/format'
import { orderStatusMeta } from '../lib/statuses'

const statusValues = Object.values(OrderStatus)

export function OrdersPage() {
  const [params, setParams] = useSearchParams()
  const navigate = useNavigate()
  const page = Math.max(1, Number(params.get('sayfa')) || 1)
  const q = params.get('q') ?? ''
  const statusParam = Number(params.get('durum'))
  const status = statusValues.find((s) => s === statusParam) ?? null
  const from = isDayString(params.get('from')) ? params.get('from')! : ''
  const to = isDayString(params.get('to')) ? params.get('to')! : ''

  const orders = useQuery({
    queryKey: ['admin', 'orders', 'list', { q, status, from, to, page }],
    queryFn: ({ signal }) => adminApi.orders({ q: q.trim(), status, from, to, page }, signal),
    placeholderData: keepPreviousData,
  })

  function applyFilters(form: HTMLFormElement) {
    const data = new FormData(form)
    const next = new URLSearchParams()
    for (const [field, key] of [['q', 'q'], ['status', 'durum'], ['from', 'from'], ['to', 'to']] as const) {
      const value = String(data.get(field) ?? '').trim()
      if (value) next.set(key, value)
    }
    setParams(next)
  }

  const setPage = (p: number) => {
    const next = new URLSearchParams(params)
    next.set('sayfa', String(p))
    setParams(next)
  }

  const hasFilters = !!(q || status || from || to)

  return (
    <section aria-labelledby="orders-title" className="flex flex-col gap-4">
      <h1 id="orders-title" className="text-xl font-bold">Siparişler</h1>

      <form
        key={params.toString()}
        role="search"
        aria-label="Sipariş filtreleri"
        onSubmit={(e) => {
          e.preventDefault()
          applyFilters(e.currentTarget)
        }}
        className="grid gap-3 rounded-xl border border-line bg-white p-4 sm:grid-cols-2 lg:grid-cols-4"
      >
        <Field label="Sipariş no veya e-posta" name="q" type="search" defaultValue={q} placeholder="TZ-… veya ad@ornek.com" />
        <SelectField label="Durum" name="status" defaultValue={status ?? ''}>
          <option value="">Tümü</option>
          {statusValues.map((s) => <option key={s} value={s}>{orderStatusMeta[s].label}</option>)}
        </SelectField>
        <Field label="Başlangıç tarihi" name="from" type="date" defaultValue={from} />
        <Field label="Bitiş tarihi" name="to" type="date" defaultValue={to} />
        <div className="flex gap-2 sm:col-span-2 lg:col-span-4">
          <button className="rounded-lg bg-brand-700 px-4 py-2 text-sm font-semibold text-white hover:bg-brand-800">Filtrele</button>
          {hasFilters && (
            <button type="button" onClick={() => setParams({})} className="rounded-lg border border-line px-4 py-2 text-sm hover:bg-surface">Temizle</button>
          )}
        </div>
      </form>

      {orders.isPending ? (
        <LoadingBlock />
      ) : orders.isError ? (
        <ErrorState error={orders.error} onRetry={() => void orders.refetch()} />
      ) : orders.data.items.length === 0 ? (
        <p className="rounded-xl border border-line bg-white px-6 py-16 text-center font-semibold">Eşleşen sipariş bulunamadı.</p>
      ) : (
        <div aria-busy={orders.isFetching}>
          <p className="mb-2 text-sm text-muted">{orders.data.totalCount.toLocaleString('tr-TR')} sipariş</p>
          <TableShell caption="Siparişler">
            <thead>
              <tr>
                <Th>Sipariş no</Th><Th>Müşteri</Th><Th>Tarih</Th><Th>Durum</Th>
                <Th className="text-right">Tutar</Th><Th className="text-right">Ürün</Th><Th className="text-right">Satıcı</Th><Th>Kupon</Th>
              </tr>
            </thead>
            <tbody>
              {orders.data.items.map((o) => (
                <tr
                  key={o.orderNumber}
                  className={`${rowClass} cursor-pointer hover:bg-surface`}
                  onClick={() => void navigate(`/siparisler/${encodeURIComponent(o.orderNumber)}`)}
                >
                  <th scope="row" className="px-3 py-3 text-left font-semibold">
                    <Link to={`/siparisler/${encodeURIComponent(o.orderNumber)}`} onClick={(e) => e.stopPropagation()} className="text-brand-700 underline">{o.orderNumber}</Link>
                  </th>
                  <td className={tdClass}>
                    <div>{o.customerName}</div>
                    <div className="text-xs text-muted">{o.customerEmail}</div>
                  </td>
                  <td className={`${tdClass} whitespace-nowrap`}>{formatDateTime(o.placedAt)}</td>
                  <td className={tdClass}><MetaBadge value={o.status} meta={orderStatusMeta} /></td>
                  <td className={`${tdClass} whitespace-nowrap text-right font-semibold`}>{formatPrice(o.grandTotal)}</td>
                  <td className={`${tdClass} text-right`}>{o.itemCount}</td>
                  <td className={`${tdClass} text-right`}>{o.sellerCount}</td>
                  <td className={tdClass}>{o.couponCode ?? <span className="text-muted">—</span>}</td>
                </tr>
              ))}
            </tbody>
          </TableShell>
          <Pagination page={orders.data.page} totalPages={orders.data.totalPages} onChange={setPage} />
        </div>
      )}
    </section>
  )
}
