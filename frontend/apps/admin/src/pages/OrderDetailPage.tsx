import { useQuery } from '@tanstack/react-query'
import type { ReactNode } from 'react'
import { Link, useParams } from 'react-router'
import { adminApi } from '../api/admin'
import { ErrorState, LoadingBlock } from '../components/ErrorState'
import { MetaBadge } from '../components/StatusBadge'
import { rowClass, RowTh, TableShell, tdClass, Th } from '../components/Table'
import { formatDateTime } from '../lib/dates'
import { formatPrice } from '../lib/format'
import { orderStatusMeta, paymentStatusMeta, shipmentStatusMeta } from '../lib/statuses'

export function OrderDetailPage() {
  const { orderNumber = '' } = useParams()
  const order = useQuery({
    queryKey: ['admin', 'orders', 'detail', orderNumber],
    queryFn: ({ signal }) => adminApi.order(orderNumber, signal),
  })

  const back = <Link to="/siparisler" className="text-sm font-semibold text-brand-700 underline">← Siparişlere dön</Link>

  if (order.isPending) return <LoadingBlock />
  if (order.isError) return <div className="flex flex-col gap-4">{back}<ErrorState error={order.error} onRetry={() => void order.refetch()} /></div>

  const o = order.data
  return (
    <article aria-labelledby="order-title" className="flex flex-col gap-6">
      <div className="flex flex-col gap-2">
        {back}
        <div className="flex flex-wrap items-center gap-3">
          <h1 id="order-title" className="text-xl font-bold">{o.orderNumber}</h1>
          <MetaBadge value={o.status} meta={orderStatusMeta} />
        </div>
      </div>

      <section aria-labelledby="summary-title" className="grid gap-4 lg:grid-cols-2">
        <h2 id="summary-title" className="sr-only">Sipariş özeti</h2>
        <dl className="grid grid-cols-[auto_1fr] gap-x-4 gap-y-2 rounded-xl border border-line bg-white p-4 text-sm">
          <Row label="Müşteri">{o.customerName}</Row>
          <Row label="E-posta"><span className="break-all">{o.customerEmail}</span></Row>
          <Row label="Teslimat şehri">{o.shippingCity ?? '—'}</Row>
          <Row label="Sipariş tarihi">{formatDateTime(o.placedAt)}</Row>
          <Row label="Ödeme tarihi">{o.paidAt ? formatDateTime(o.paidAt) : '—'}</Row>
          <Row label="Kupon">{o.couponCode ?? '—'}</Row>
        </dl>
        <dl className="grid grid-cols-[1fr_auto] gap-x-4 gap-y-2 rounded-xl border border-line bg-white p-4 text-sm">
          <Row label="Ara toplam">{formatPrice(o.subtotal)}</Row>
          <Row label="İndirim">{o.discountTotal > 0 ? `−${formatPrice(o.discountTotal)}` : formatPrice(0)}</Row>
          <Row label="Kargo">{formatPrice(o.shippingTotal)}</Row>
          <dt className="border-t border-line pt-2 font-semibold">Genel toplam</dt>
          <dd className="border-t border-line pt-2 text-right font-bold">{formatPrice(o.grandTotal)}</dd>
        </dl>
      </section>

      <section aria-labelledby="items-title">
        <h2 id="items-title" className="mb-2 font-semibold">Kalemler</h2>
        <TableShell caption="Sipariş kalemleri">
          <thead>
            <tr>
              <Th>Ürün</Th><Th>SKU</Th><Th>Satıcı</Th><Th className="text-right">Adet</Th><Th className="text-right">Birim fiyat</Th>
              <Th className="text-right">İndirim</Th><Th className="text-right">Satır toplamı</Th><Th className="text-right">Komisyon</Th>
            </tr>
          </thead>
          <tbody>
            {o.items.map((item) => (
              <tr key={`${item.sku}-${item.sellerName}`} className={rowClass}>
                <RowTh>
                  {item.productName}
                  {item.variantLabel && <span className="block text-xs font-normal text-muted">{item.variantLabel}</span>}
                </RowTh>
                <td className={tdClass}>{item.sku}</td>
                <td className={tdClass}>{item.sellerName}</td>
                <td className={`${tdClass} text-right`}>{item.quantity}</td>
                <td className={`${tdClass} text-right`}>{formatPrice(item.unitPrice)}</td>
                <td className={`${tdClass} text-right`}>{formatPrice(item.discountAmount)}</td>
                <td className={`${tdClass} text-right font-semibold`}>{formatPrice(item.lineTotal)}</td>
                <td className={`${tdClass} text-right`}>%{item.commissionRate.toLocaleString('tr-TR')}</td>
              </tr>
            ))}
          </tbody>
        </TableShell>
      </section>

      <section aria-labelledby="payments-title">
        <h2 id="payments-title" className="mb-2 font-semibold">Ödemeler</h2>
        {o.payments.length === 0 ? (
          <p className="text-sm text-muted">Ödeme kaydı yok.</p>
        ) : (
          <TableShell caption="Ödemeler">
            <thead>
              <tr><Th>Sağlayıcı</Th><Th>Referans</Th><Th className="text-right">Tutar</Th><Th className="text-right">Taksit</Th><Th>Durum</Th><Th>Oluşturulma</Th><Th>Tamamlanma</Th></tr>
            </thead>
            <tbody>
              {o.payments.map((p, i) => (
                <tr key={`${p.providerPaymentId ?? 'p'}-${i}`} className={rowClass}>
                  <RowTh>{p.provider}</RowTh>
                  <td className={tdClass}>{p.providerPaymentId ?? '—'}</td>
                  <td className={`${tdClass} text-right`}>{formatPrice(p.amount)}</td>
                  <td className={`${tdClass} text-right`}>{p.installmentCount}</td>
                  <td className={tdClass}>
                    <MetaBadge value={p.status} meta={paymentStatusMeta} />
                    {p.failureReason && <span className="mt-1 block max-w-60 whitespace-normal text-xs text-red-700">{p.failureReason}</span>}
                  </td>
                  <td className={`${tdClass} whitespace-nowrap`}>{formatDateTime(p.createdAt)}</td>
                  <td className={`${tdClass} whitespace-nowrap`}>{p.completedAt ? formatDateTime(p.completedAt) : '—'}</td>
                </tr>
              ))}
            </tbody>
          </TableShell>
        )}
      </section>

      <section aria-labelledby="shipments-title">
        <h2 id="shipments-title" className="mb-2 font-semibold">Kargolar</h2>
        {o.shipments.length === 0 ? (
          <p className="text-sm text-muted">Henüz kargo kaydı yok.</p>
        ) : (
          <TableShell caption="Kargolar">
            <thead>
              <tr><Th>Satıcı</Th><Th>Firma</Th><Th>Takip no</Th><Th>Durum</Th><Th>Gönderim</Th></tr>
            </thead>
            <tbody>
              {o.shipments.map((s, i) => (
                <tr key={`${s.trackingNumber ?? 's'}-${i}`} className={rowClass}>
                  <RowTh>{s.sellerName}</RowTh>
                  <td className={tdClass}>{s.carrier}</td>
                  <td className={tdClass}>{s.trackingNumber ?? '—'}</td>
                  <td className={tdClass}><MetaBadge value={s.status} meta={shipmentStatusMeta} /></td>
                  <td className={`${tdClass} whitespace-nowrap`}>{s.shippedAt ? formatDateTime(s.shippedAt) : '—'}</td>
                </tr>
              ))}
            </tbody>
          </TableShell>
        )}
      </section>

      <section aria-labelledby="history-title">
        <h2 id="history-title" className="mb-2 font-semibold">Durum geçmişi</h2>
        {o.history.length === 0 ? (
          <p className="text-sm text-muted">Geçmiş kaydı yok.</p>
        ) : (
          <TableShell caption="Durum geçmişi">
            <thead>
              <tr><Th>Tarih</Th><Th>Önceki durum</Th><Th>Yeni durum</Th><Th>Not</Th></tr>
            </thead>
            <tbody>
              {o.history.map((h, i) => (
                <tr key={`${h.changedAt}-${i}`} className={rowClass}>
                  <RowTh className="whitespace-nowrap font-medium">{formatDateTime(h.changedAt)}</RowTh>
                  <td className={tdClass}>{h.from ? <MetaBadge value={h.from} meta={orderStatusMeta} /> : <span className="text-muted">—</span>}</td>
                  <td className={tdClass}><MetaBadge value={h.to} meta={orderStatusMeta} /></td>
                  <td className={`${tdClass} min-w-48 whitespace-normal`}>{h.note ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </TableShell>
        )}
      </section>
    </article>
  )
}

function Row({ label, children }: { label: string; children: ReactNode }) {
  return (
    <>
      <dt className="text-muted">{label}</dt>
      <dd className="min-w-0 text-right sm:text-left lg:text-right">{children}</dd>
    </>
  )
}
