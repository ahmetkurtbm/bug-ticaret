import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useRef, useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router'
import { ApiError } from '../api/client'
import { addressApi, orderApi } from '../api/orders'
import type { Address, NewAddress } from '../api/types'
import { cartQueryKey, useCart } from '../cart/useCart'
import { ErrorState } from '../components/ErrorState'
import { formatPrice } from '../lib/format'
import { Field } from './authUi'

const INSTALLMENTS = [1, 2, 3, 6, 9, 12] as const
const addressesKey = ['addresses'] as const

export function CheckoutPage() {
  const cart = useCart()
  const addresses = useQuery({ queryKey: addressesKey, queryFn: ({ signal }) => addressApi.list(signal) })
  const [selectedId, setSelectedId] = useState<number | null>(null)
  const [installments, setInstallments] = useState<number>(1)
  const [adding, setAdding] = useState(false)
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  // Aynı ödeme denemesinde anahtar sabit: butona iki kez basılsa da tek sipariş açılır.
  const idempotencyKey = useRef(crypto.randomUUID())

  const place = useMutation({
    mutationFn: (addressId: number) => orderApi.place(addressId, installments, idempotencyKey.current),
    onSuccess: (result) => {
      void queryClient.invalidateQueries({ queryKey: cartQueryKey })
      void queryClient.invalidateQueries({ queryKey: ['orders'] })
      if (result.checkoutUrl) window.location.assign(result.checkoutUrl)
      else navigate(`/hesabim/siparisler/${result.orderNumber}`, { state: { paymentError: result.paymentError } })
    },
    onError: () => {
      // Sipariş açılamadıysa yeni deneme yeni anahtarla gider.
      idempotencyKey.current = crypto.randomUUID()
    },
  })

  if (cart.isPending || addresses.isPending) return <div className="h-72 animate-pulse rounded-xl bg-line/60" aria-busy="true" />
  if (cart.isError) return <ErrorState error={cart.error} onRetry={() => void cart.refetch()} />
  if (addresses.isError) return <ErrorState error={addresses.error} onRetry={() => void addresses.refetch()} />

  const data = cart.data
  if (data.lines.length === 0 && !place.isSuccess) {
    return (
      <div className="rounded-xl border border-line bg-white px-6 py-16 text-center">
        <h1 className="text-xl font-bold">Sepetiniz boş</h1>
        <Link to="/" className="mt-6 inline-block rounded-lg bg-brand-700 px-5 py-2.5 text-sm font-semibold text-white">Alışverişe başla</Link>
      </div>
    )
  }

  const list = addresses.data
  const effectiveId = selectedId ?? list.find((a) => a.isDefault)?.id ?? list[0]?.id ?? null
  const showForm = adding || list.length === 0
  const monthly = data.grandTotal / installments

  function onSubmit(e: FormEvent) {
    e.preventDefault()
    if (effectiveId !== null && !place.isPending) place.mutate(effectiveId)
  }

  return (
    <div className="grid gap-6 lg:grid-cols-[1fr_22rem]">
      <div className="flex flex-col gap-6">
        <h1 className="text-xl font-bold">Ödeme</h1>

        <section aria-labelledby="address-title" className="rounded-xl border border-line bg-white p-5">
          <div className="mb-3 flex items-center justify-between">
            <h2 id="address-title" className="font-bold">Teslimat adresi</h2>
            {list.length > 0 && !adding && (
              <button type="button" onClick={() => setAdding(true)} className="text-sm font-semibold text-brand-700 hover:underline">
                Yeni adres ekle
              </button>
            )}
          </div>
          {list.length > 0 && (
            <fieldset className="grid gap-3 sm:grid-cols-2">
              <legend className="sr-only">Kayıtlı adresler</legend>
              {list.map((a) => (
                <AddressOption key={a.id} address={a} checked={a.id === effectiveId} onSelect={() => setSelectedId(a.id)} />
              ))}
            </fieldset>
          )}
          {showForm && (
            <AddressForm
              onCancel={list.length > 0 ? () => setAdding(false) : undefined}
              onCreated={(a) => {
                setSelectedId(a.id)
                setAdding(false)
              }}
            />
          )}
        </section>

        <section aria-labelledby="installment-title" className="rounded-xl border border-line bg-white p-5">
          <h2 id="installment-title" className="mb-3 font-bold">Taksit</h2>
          <label htmlFor="installments" className="sr-only">Taksit sayısı</label>
          <select
            id="installments"
            value={installments}
            onChange={(e) => setInstallments(Number(e.target.value))}
            className="w-full rounded-lg border border-line px-3 py-2.5 sm:w-72"
          >
            {INSTALLMENTS.map((n) => (
              <option key={n} value={n}>
                {n === 1 ? 'Tek çekim' : `${n} taksit × ${formatPrice(data.grandTotal / n)}`}
              </option>
            ))}
          </select>
          <p className="mt-2 text-xs text-muted">Kart bilgilerinizi bir sonraki adımda, ödeme sayfasında gireceksiniz.</p>
        </section>
      </div>

      <aside className="self-start lg:sticky lg:top-32">
        <form onSubmit={onSubmit} className="rounded-xl border border-line bg-white p-5">
          <h2 className="mb-3 font-bold">Sipariş özeti</h2>
          <ul className="mb-3 flex flex-col gap-2 text-sm">
            {data.lines.map((l) => (
              <li key={l.id} className="flex justify-between gap-3">
                <span className="line-clamp-1 text-muted">{l.quantity} × {l.productName}</span>
                <span className="shrink-0">{formatPrice(l.lineTotal)}</span>
              </li>
            ))}
          </ul>
          <dl className="flex flex-col gap-2 border-t border-line pt-3 text-sm">
            <Row label="Ürünler" value={formatPrice(data.subtotal)} />
            {data.discountTotal > 0 && <Row label={`İndirim${data.couponCode ? ` (${data.couponCode})` : ''}`} value={`-${formatPrice(data.discountTotal)}`} />}
            <Row label="Kargo" value={data.shippingTotal === 0 ? 'Bedava' : formatPrice(data.shippingTotal)} />
            <div className="mt-2 flex justify-between border-t border-line pt-3 text-base font-bold">
              <dt>Toplam</dt>
              <dd>{formatPrice(data.grandTotal)}</dd>
            </div>
            {installments > 1 && <p className="text-xs text-muted">{installments} × {formatPrice(monthly)}</p>}
          </dl>
          {place.error && (
            <p className="mt-3 rounded-lg bg-red-50 px-3 py-2 text-sm text-red-800" role="alert">
              {place.error instanceof ApiError ? place.error.message : 'Sipariş oluşturulamadı.'}
              {place.error instanceof ApiError && (place.error.code === 'insufficient_stock' || place.error.code === 'variant_unavailable') && (
                <> <Link to="/sepet" className="font-semibold underline">Sepete dön</Link></>
              )}
            </p>
          )}
          <button
            type="submit"
            disabled={effectiveId === null || place.isPending || place.isSuccess}
            className="mt-4 w-full rounded-lg bg-brand-700 py-3 font-semibold text-white disabled:opacity-60"
          >
            {place.isPending || place.isSuccess ? 'Ödeme sayfasına yönlendiriliyor…' : 'Ödemeye geç'}
          </button>
          {effectiveId === null && <p className="mt-2 text-xs text-muted">Devam etmek için bir teslimat adresi ekleyin.</p>}
        </form>
      </aside>
    </div>
  )
}

function AddressOption({ address, checked, onSelect }: { address: Address; checked: boolean; onSelect: () => void }) {
  return (
    <label className={`flex cursor-pointer gap-3 rounded-lg border p-3 text-sm ${checked ? 'border-brand-700 bg-brand-50' : 'border-line'}`}>
      <input type="radio" name="address" checked={checked} onChange={onSelect} className="mt-1 accent-brand-700" />
      <span>
        <span className="block font-semibold">{address.title}</span>
        <span className="block">{address.fullName}</span>
        <span className="block text-muted">{address.line}</span>
        <span className="block text-muted">{address.district} / {address.city}</span>
      </span>
    </label>
  )
}

const emptyAddress: NewAddress = { title: 'Ev', fullName: '', phone: '', city: '', district: '', line: '', postalCode: null, isDefault: false }

function AddressForm({ onCreated, onCancel }: { onCreated: (a: Address) => void; onCancel?: () => void }) {
  const [form, setForm] = useState<NewAddress>(emptyAddress)
  const queryClient = useQueryClient()
  const create = useMutation({
    mutationFn: addressApi.create,
    onSuccess: async (a) => {
      await queryClient.invalidateQueries({ queryKey: addressesKey })
      onCreated(a)
    },
  })
  const errors = create.error instanceof ApiError ? create.error.fieldErrors : {}
  const set = (key: keyof NewAddress) => (e: { target: { value: string } }) => setForm((f) => ({ ...f, [key]: e.target.value }))

  function onSubmit(e: FormEvent) {
    e.preventDefault()
    create.mutate({ ...form, phone: form.phone.replace(/\s/g, ''), postalCode: form.postalCode?.trim() || null })
  }

  // Ana formun içinde form olmaz; bu yüzden ayrı bir <form> ve özet tarafı ayrı.
  return (
    <form onSubmit={onSubmit} className="mt-4 grid gap-3 sm:grid-cols-2" noValidate>
      <Field label="Adres başlığı" name="title" value={form.title} onChange={set('title')} error={errors.title} />
      <Field label="Ad soyad" name="fullName" value={form.fullName} onChange={set('fullName')} error={errors.fullName} autoComplete="name" />
      <Field label="Cep telefonu" name="phone" value={form.phone} onChange={set('phone')} error={errors.phone} inputMode="tel" autoComplete="tel" placeholder="05XX XXX XX XX" />
      <Field label="Posta kodu (isteğe bağlı)" name="postalCode" value={form.postalCode ?? ''} onChange={set('postalCode')} error={errors.postalCode} inputMode="numeric" autoComplete="postal-code" />
      <Field label="İl" name="city" value={form.city} onChange={set('city')} error={errors.city} autoComplete="address-level1" />
      <Field label="İlçe" name="district" value={form.district} onChange={set('district')} error={errors.district} autoComplete="address-level2" />
      <div className="sm:col-span-2">
        <Field label="Açık adres" name="line" value={form.line} onChange={set('line')} error={errors.line} autoComplete="street-address" placeholder="Mahalle, cadde, bina ve daire no" />
      </div>
      {create.error && !(create.error instanceof ApiError && Object.keys(errors).length > 0) && (
        <p className="text-sm text-red-700 sm:col-span-2" role="alert">{create.error instanceof ApiError ? create.error.message : 'Adres kaydedilemedi.'}</p>
      )}
      <div className="flex gap-3 sm:col-span-2">
        <button disabled={create.isPending} className="rounded-lg bg-brand-700 px-5 py-2.5 text-sm font-semibold text-white disabled:opacity-60">
          Adresi kaydet
        </button>
        {onCancel && (
          <button type="button" onClick={onCancel} className="rounded-lg px-4 py-2.5 text-sm text-muted hover:text-ink">Vazgeç</button>
        )}
      </div>
    </form>
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
