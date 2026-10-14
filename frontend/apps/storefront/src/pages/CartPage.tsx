import { useState, type FormEvent } from 'react'
import { Link } from 'react-router'
import { ApiError } from '../api/client'
import type { CartLine } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import { useApplyCoupon, useCart, useRemoveCartItem, useRemoveCoupon, useUpdateCartItem } from '../cart/useCart'
import { useQuantityStepper } from '../cart/useQuantityStepper'
import { ErrorState } from '../components/ErrorState'
import { formatPrice } from '../lib/format'

export function CartPage() {
  const cart = useCart()
  const { user } = useAuth()

  if (cart.isPending) return <div className="h-72 animate-pulse rounded-xl bg-line/60" aria-busy="true" />
  if (cart.isError) return <ErrorState error={cart.error} onRetry={() => void cart.refetch()} />

  const data = cart.data
  if (data.lines.length === 0) {
    return (
      <div className="rounded-xl border border-line bg-white px-6 py-16 text-center">
        <h1 className="text-xl font-bold">Sepetiniz boş</h1>
        <p className="mt-2 text-muted">Beğendiğiniz ürünleri sepete ekleyin, burada görün.</p>
        <Link to="/" className="mt-6 inline-block rounded-lg bg-brand-700 px-5 py-2.5 text-sm font-semibold text-white">Alışverişe başla</Link>
      </div>
    )
  }

  return (
    <div className="grid gap-6 lg:grid-cols-[1fr_22rem]">
      <section aria-labelledby="cart-title">
        <h1 id="cart-title" className="mb-4 text-xl font-bold">Sepetim ({data.itemCount} ürün)</h1>
        {data.freeShippingRemaining > 0 && (
          <p className="mb-3 rounded-lg bg-brand-50 px-4 py-2.5 text-sm text-brand-800">
            <strong>{formatPrice(data.freeShippingRemaining)}</strong> daha ekleyin, kargo bedava olsun.
          </p>
        )}
        <ul className="divide-y divide-line rounded-xl border border-line bg-white">
          {data.lines.map((line) => (
            <CartLineRow key={line.id} line={line} />
          ))}
        </ul>
      </section>

      <aside className="flex flex-col gap-4 self-start lg:sticky lg:top-32">
        <CouponBox code={data.couponCode} error={data.couponError} />
        <section aria-labelledby="summary-title" className="rounded-xl border border-line bg-white p-5">
          <h2 id="summary-title" className="mb-3 font-bold">Sipariş özeti</h2>
          <dl className="flex flex-col gap-2 text-sm">
            <Row label="Ürünler" value={formatPrice(data.subtotal)} />
            {data.discountTotal > 0 && <Row label="İndirim" value={`-${formatPrice(data.discountTotal)}`} accent />}
            <Row label="Kargo" value={data.shippingTotal === 0 ? 'Bedava' : formatPrice(data.shippingTotal)} />
            <div className="mt-2 flex justify-between border-t border-line pt-3 text-base font-bold">
              <dt>Toplam</dt>
              <dd>{formatPrice(data.grandTotal)}</dd>
            </div>
            <p className="text-xs text-muted">KDV dahil ({formatPrice(data.vatTotal)} KDV)</p>
          </dl>
          <Link to="/odeme" className="mt-4 block w-full rounded-lg bg-brand-700 py-3 text-center font-semibold text-white hover:bg-brand-800">
            Siparişi tamamla
          </Link>
          {!user && <p className="mt-2 text-center text-xs text-muted">Devam etmek için giriş yapmanız istenecek; sepetiniz korunur.</p>}
        </section>
      </aside>
    </div>
  )
}

export function CartLineRow({ line }: { line: CartLine }) {
  const update = useUpdateCartItem()
  const remove = useRemoveCartItem()
  const max = Math.min(20, line.availableQuantity)
  const stepper = useQuantityStepper(line.quantity, max, (quantity) => update.mutateAsync({ itemId: line.id, quantity }))
  const error = update.error ?? remove.error

  return (
    <li className="flex gap-4 p-4">
      <Link to={`/urun/${line.productSlug}`} className="h-24 w-24 shrink-0 overflow-hidden rounded-lg bg-surface">
        {line.imageUrl && <img src={line.imageUrl} alt="" className="h-full w-full object-cover" />}
      </Link>
      <div className="flex flex-1 flex-col gap-1">
        <Link to={`/urun/${line.productSlug}`} className="font-medium hover:text-brand-700">{line.productName}</Link>
        <p className="text-sm text-muted">
          {line.variantLabel && <>{line.variantLabel} · </>}
          {line.sellerName}
        </p>
        {line.priceChanged && <p className="text-xs font-medium text-accent-500">Fiyat sepete eklediğinizden beri değişti.</p>}
        <div className="mt-auto flex items-center justify-between gap-3">
          <div className="flex items-center rounded-lg border border-line" aria-label="Adet">
            <button
              aria-label="Azalt"
              disabled={remove.isPending || stepper.quantity <= 1}
              onClick={() => stepper.change(-1)}
              className="h-9 w-9 disabled:opacity-40"
            >
              −
            </button>
            <span className="w-8 text-center text-sm font-semibold" aria-live="polite">{stepper.quantity}</span>
            <button
              aria-label="Artır"
              disabled={remove.isPending || stepper.quantity >= max}
              onClick={() => stepper.change(1)}
              className="h-9 w-9 disabled:opacity-40"
            >
              +
            </button>
          </div>
          <div className="text-right">
            <p className="font-bold">{formatPrice(line.lineTotal)}</p>
            {line.discountAmount > 0 && <p className="text-xs text-muted line-through">{formatPrice(line.unitPrice * line.quantity)}</p>}
          </div>
        </div>
        <div className="flex items-center justify-between">
          {error && <p className="text-xs text-accent-500" role="alert">{error instanceof ApiError ? error.message : 'İşlem yapılamadı.'}</p>}
          <button disabled={remove.isPending} onClick={() => remove.mutate(line.id)} className="ml-auto text-xs text-muted underline hover:text-ink">
            Kaldır
          </button>
        </div>
      </div>
    </li>
  )
}

function CouponBox({ code, error }: { code: string | null; error: string | null }) {
  const [value, setValue] = useState('')
  const apply = useApplyCoupon()
  const remove = useRemoveCoupon()

  function onSubmit(e: FormEvent) {
    e.preventDefault()
    if (value.trim()) apply.mutate(value.trim(), { onSuccess: () => setValue('') })
  }

  return (
    <section aria-labelledby="coupon-title" className="rounded-xl border border-line bg-white p-5">
      <h2 id="coupon-title" className="mb-3 font-bold">İndirim kodu</h2>
      {code ? (
        <div className="flex items-center justify-between rounded-lg bg-brand-50 px-3 py-2 text-sm">
          <span className="font-semibold text-brand-800">{code}</span>
          <button onClick={() => remove.mutate()} className="text-xs underline">Kaldır</button>
        </div>
      ) : (
        <form onSubmit={onSubmit} className="flex gap-2">
          <label htmlFor="coupon" className="sr-only">Kupon kodu</label>
          <input id="coupon" value={value} onChange={(e) => setValue(e.target.value)} placeholder="Kupon kodu" className="flex-1 rounded-lg border border-line px-3 py-2 text-sm uppercase placeholder:normal-case" />
          <button disabled={apply.isPending} className="rounded-lg border border-brand-700 px-4 text-sm font-semibold text-brand-700 hover:bg-brand-50">
            Uygula
          </button>
        </form>
      )}
      {(error || apply.error) && (
        <p className="mt-2 text-xs text-accent-500" role="alert">
          {apply.error instanceof ApiError ? apply.error.message : error}
        </p>
      )}
    </section>
  )
}

function Row({ label, value, accent }: { label: string; value: string; accent?: boolean }) {
  return (
    <div className="flex justify-between">
      <dt className="text-muted">{label}</dt>
      <dd className={accent ? 'font-semibold text-brand-700' : ''}>{value}</dd>
    </div>
  )
}
