import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { Link, useParams } from 'react-router'
import { ApiError } from '../api/client'
import { sellerApi } from '../api/seller'
import { ProductStatus, type SellerProductDetail, type SellerVariant } from '../api/types'
import { CategorySelect } from '../components/CategorySelect'
import { ErrorState, LoadingBlock } from '../components/ErrorState'
import { Alert, Field, primaryButton, secondaryButton, TextAreaField } from '../components/Field'
import { ProductStatusBadge } from '../components/StatusBadge'
import { parsePrice, formatPriceInput } from '../lib/price'
import { NotFoundPage } from './NotFoundPage'

const PRICE_ERROR = 'Fiyatı 1.249,90 biçiminde yazın (en çok 2 ondalık).'

function errorMessage(error: unknown, fallback: string) {
  return error instanceof ApiError ? error.message : fallback
}

export function ProductDetailPage() {
  const params = useParams()
  const id = Number(params.id)
  const valid = Number.isInteger(id) && id > 0
  const product = useQuery({
    queryKey: ['seller', 'product', id],
    queryFn: ({ signal }) => sellerApi.product(id, signal),
    enabled: valid,
    retry: (count, error) => !(error instanceof ApiError && error.status < 500) && count < 2,
  })

  if (!valid || (product.error instanceof ApiError && product.error.status === 404)) return <NotFoundPage />
  if (product.isPending) return <LoadingBlock />
  if (product.isError) return <ErrorState error={product.error} onRetry={() => void product.refetch()} />
  return <ProductDetail product={product.data} />
}

function useProductCache(productId: number) {
  const queryClient = useQueryClient()
  return {
    store(updated: SellerProductDetail) {
      queryClient.setQueryData(['seller', 'product', productId], updated)
      void queryClient.invalidateQueries({ queryKey: ['seller', 'products'] })
      void queryClient.invalidateQueries({ queryKey: ['seller', 'dashboard'] })
    },
    replaceVariant(updated: SellerVariant) {
      queryClient.setQueryData<SellerProductDetail>(['seller', 'product', productId], (old) =>
        old ? { ...old, variants: old.variants.map((v) => (v.id === updated.id ? updated : v)) } : old,
      )
      void queryClient.invalidateQueries({ queryKey: ['seller', 'products'] })
      void queryClient.invalidateQueries({ queryKey: ['seller', 'dashboard'] })
    },
  }
}

function ProductDetail({ product }: { product: SellerProductDetail }) {
  const cache = useProductCache(product.id)
  const submit = useMutation({ mutationFn: () => sellerApi.submitProduct(product.id), onSuccess: cache.store })
  const archive = useMutation({ mutationFn: () => sellerApi.archiveProduct(product.id), onSuccess: cache.store })
  const canSubmit = product.status === ProductStatus.Draft || product.status === ProductStatus.Rejected
  const canArchive = product.status !== ProductStatus.Archived
  const actionError = submit.error ?? archive.error

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Link to="/urunler" className="text-sm text-brand-700 hover:underline">← Ürünlere dön</Link>
        <div className="mt-1 flex flex-wrap items-center gap-3">
          <h1 className="min-w-0 break-words text-xl font-bold">{product.name}</h1>
          <ProductStatusBadge status={product.status} />
        </div>
        <p className="mt-1 text-sm text-muted">{product.categoryName}{product.brandName ? ` · ${product.brandName}` : ''}</p>
      </div>

      {product.status === ProductStatus.Rejected && <Alert>Bu ürün reddedildi. Bilgileri düzeltip yeniden onaya gönderebilirsiniz.</Alert>}
      {product.status === ProductStatus.PendingApproval && <Alert tone="success">Ürün onay bekliyor.</Alert>}
      {actionError && <Alert>{errorMessage(actionError, 'İşlem yapılamadı.')}</Alert>}

      {(canSubmit || canArchive) && (
        <div className="flex flex-wrap gap-3">
          {canSubmit && (
            <button onClick={() => submit.mutate()} disabled={submit.isPending || archive.isPending} className={primaryButton}>
              {submit.isPending ? 'Gönderiliyor…' : 'Onaya gönder'}
            </button>
          )}
          {canArchive && (
            <button
              onClick={() => {
                if (window.confirm('Ürün arşivlensin mi?')) archive.mutate()
              }}
              disabled={submit.isPending || archive.isPending}
              className={secondaryButton}
            >
              {archive.isPending ? 'Arşivleniyor…' : 'Arşivle'}
            </button>
          )}
        </div>
      )}

      {product.images.length > 0 && (
        <ul className="flex gap-2 overflow-x-auto pb-1" aria-label="Ürün görselleri">
          {product.images.map((url) => (
            <li key={url} className="shrink-0">
              <img src={url} alt="" loading="lazy" className="size-24 rounded-lg bg-surface object-cover" />
            </li>
          ))}
        </ul>
      )}

      <EditForm product={product} onSaved={cache.store} />

      <section aria-labelledby="variants-title">
        <h2 id="variants-title" className="mb-3 font-bold">Varyantlar</h2>
        <ul className="flex flex-col gap-4">
          {product.variants.map((v) => (
            <li key={v.id}>
              <VariantRow variant={v} onChanged={cache.replaceVariant} />
            </li>
          ))}
        </ul>
      </section>
    </div>
  )
}

function EditForm({ product, onSaved }: { product: SellerProductDetail; onSaved: (p: SellerProductDetail) => void }) {
  const [name, setName] = useState(product.name)
  const [description, setDescription] = useState(product.description ?? '')
  const [categoryId, setCategoryId] = useState(String(product.categoryId))
  const update = useMutation({
    mutationFn: () =>
      sellerApi.updateProduct(product.id, {
        name: name.trim(),
        description: description.trim() || null,
        categoryId: Number(categoryId) || 0,
        brandId: product.brandId,
      }),
    onSuccess: onSaved,
  })
  const errors = update.error instanceof ApiError ? update.error.fieldErrors : {}
  const general = update.error && Object.keys(errors).length === 0 ? errorMessage(update.error, 'Kaydedilemedi.') : null

  function onSubmit(e: FormEvent) {
    e.preventDefault()
    if (!update.isPending) update.mutate()
  }

  return (
    <form onSubmit={onSubmit} noValidate aria-busy={update.isPending} aria-labelledby="edit-title" className="flex flex-col gap-4 rounded-xl border border-line bg-white p-4 sm:p-5">
      <h2 id="edit-title" className="font-bold">Ürün bilgileri</h2>
      {general && <Alert>{general}</Alert>}
      {update.isSuccess && <Alert tone="success">Ürün bilgileri kaydedildi.</Alert>}
      <Field label="Ürün adı" name="name" value={name} onChange={(e) => setName(e.target.value)} error={errors.name} maxLength={200} />
      <TextAreaField label="Açıklama" name="description" rows={5} value={description} onChange={(e) => setDescription(e.target.value)} error={errors.description} maxLength={5000} />
      <CategorySelect value={categoryId} onChange={setCategoryId} error={errors.categoryId} />
      <div>
        <button disabled={update.isPending} className={primaryButton}>{update.isPending ? 'Kaydediliyor…' : 'Bilgileri kaydet'}</button>
      </div>
    </form>
  )
}

function VariantRow({ variant, onChanged }: { variant: SellerVariant; onChanged: (v: SellerVariant) => void }) {
  const [price, setPrice] = useState(formatPriceInput(variant.price))
  const [listPrice, setListPrice] = useState(formatPriceInput(variant.listPrice))
  const [isActive, setIsActive] = useState(variant.isActive)
  const [priceError, setPriceError] = useState<{ price?: string; listPrice?: string }>({})
  const [quantity, setQuantity] = useState(String(variant.quantity))
  const [note, setNote] = useState('')
  const [stockError, setStockError] = useState<string | null>(null)

  const save = useMutation({
    mutationFn: (body: { price: number; listPrice: number | null; isActive: boolean }) => sellerApi.updateVariant(variant.id, body),
    onSuccess: (updated) => {
      setPrice(formatPriceInput(updated.price))
      setListPrice(formatPriceInput(updated.listPrice))
      setIsActive(updated.isActive)
      onChanged(updated)
    },
  })
  const stock = useMutation({
    mutationFn: (body: { quantity: number; note: string | null }) => sellerApi.setStock(variant.id, body),
    onSuccess: (updated) => {
      setQuantity(String(updated.quantity))
      setNote('')
      onChanged(updated)
    },
  })

  const saveErrors = save.error instanceof ApiError ? save.error.fieldErrors : {}
  const stockErrors = stock.error instanceof ApiError ? stock.error.fieldErrors : {}
  const label = [variant.color, variant.size].filter(Boolean).join(' / ')

  function onSaveSubmit(e: FormEvent) {
    e.preventDefault()
    if (save.isPending) return
    save.reset()
    const parsedPrice = parsePrice(price)
    const parsedList = listPrice.trim() ? parsePrice(listPrice) : null
    const found = {
      price: parsedPrice === null ? PRICE_ERROR : undefined,
      listPrice: listPrice.trim() && parsedList === null ? PRICE_ERROR : undefined,
    }
    setPriceError(found)
    if (parsedPrice === null || found.listPrice) return
    save.mutate({ price: parsedPrice, listPrice: parsedList, isActive })
  }

  function onStockSubmit(e: FormEvent) {
    e.preventDefault()
    if (stock.isPending) return
    stock.reset()
    if (!/^\d+$/.test(quantity.trim())) {
      setStockError('Stok 0 ya da daha büyük bir tam sayı olmalı.')
      return
    }
    setStockError(null)
    stock.mutate({ quantity: Number(quantity.trim()), note: note.trim() || null })
  }

  const idBase = `variant-${variant.id}`
  const saveGeneral = save.error && Object.keys(saveErrors).length === 0 ? errorMessage(save.error, 'Kaydedilemedi.') : null
  const stockGeneral = stock.error && Object.keys(stockErrors).length === 0 ? errorMessage(stock.error, 'Stok güncellenemedi.') : null

  return (
    <article aria-label={`Varyant ${variant.sku}`} className={`rounded-xl border border-line bg-white p-4 sm:p-5 ${variant.isActive ? '' : 'opacity-90'}`}>
      <header className="mb-3 flex flex-wrap items-baseline gap-x-3 gap-y-1">
        <h3 className="break-all font-semibold">{variant.sku}</h3>
        {label && <span className="text-sm text-muted">{label}</span>}
        <span className="text-sm text-muted">KDV %{variant.vatRate}</span>
      </header>

      <div className="grid gap-5 lg:grid-cols-2">
        <form onSubmit={onSaveSubmit} noValidate aria-busy={save.isPending} className="flex flex-col gap-3">
          <div className="grid grid-cols-2 gap-3">
            <Field id={`${idBase}-price`} label="Fiyat (TL)" name={`${idBase}-price`} type="text" inputMode="decimal" value={price} onChange={(e) => setPrice(e.target.value)} error={priceError.price ?? saveErrors.price} />
            <Field id={`${idBase}-list`} label="Liste fiyatı (TL)" name={`${idBase}-list`} type="text" inputMode="decimal" value={listPrice} onChange={(e) => setListPrice(e.target.value)} error={priceError.listPrice ?? saveErrors.listPrice} />
          </div>
          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} className="size-4 accent-brand-700" />
            Satışta (aktif)
          </label>
          {saveGeneral && <Alert>{saveGeneral}</Alert>}
          {save.isSuccess && <Alert tone="success">Fiyat bilgisi kaydedildi.</Alert>}
          <div>
            <button disabled={save.isPending} className={secondaryButton}>{save.isPending ? 'Kaydediliyor…' : 'Fiyatı kaydet'}</button>
          </div>
        </form>

        <form onSubmit={onStockSubmit} noValidate aria-busy={stock.isPending} className="flex flex-col gap-3">
          <div className="grid grid-cols-2 gap-3">
            <Field
              id={`${idBase}-qty`}
              label="Stok adedi"
              name={`${idBase}-qty`}
              inputMode="numeric"
              value={quantity}
              onChange={(e) => setQuantity(e.target.value)}
              error={stockError ?? stockErrors.quantity}
              hint={`Ayrılmış: ${variant.reserved} adet; bundan az girilemez.`}
            />
            <Field id={`${idBase}-note`} label="Not (isteğe bağlı)" name={`${idBase}-note`} value={note} onChange={(e) => setNote(e.target.value)} maxLength={200} error={stockErrors.note} />
          </div>
          {stockGeneral && <Alert>{stockGeneral}</Alert>}
          {stock.isSuccess && <Alert tone="success">Stok güncellendi.</Alert>}
          <div>
            <button disabled={stock.isPending} className={secondaryButton}>{stock.isPending ? 'Güncelleniyor…' : 'Stoğu güncelle'}</button>
          </div>
        </form>
      </div>
    </article>
  )
}
