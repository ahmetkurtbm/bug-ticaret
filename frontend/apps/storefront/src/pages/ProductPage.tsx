import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { Link, useParams } from 'react-router'
import { catalogApi } from '../api/catalog'
import { ApiError } from '../api/client'
import type { ProductDetail, Variant } from '../api/types'
import { useAddToCart } from '../cart/useCart'
import { ErrorState } from '../components/ErrorState'
import { Rating } from '../components/Rating'
import { discountPercent, formatPrice } from '../lib/format'
import { useRecentlyViewed } from '../lib/useRecentlyViewed'

export function ProductPage() {
  const { slug = '' } = useParams()
  const product = useQuery({ queryKey: ['product', slug], queryFn: ({ signal }) => catalogApi.product(slug, signal) })

  if (product.isPending) return <div className="h-96 animate-pulse rounded-xl bg-line/60" aria-busy="true" />
  if (product.isError) return <ErrorState error={product.error} onRetry={() => void product.refetch()} />
  useRecentlyViewed(product.data.id)
  // key: farklı ürüne geçince seçili varyant ve görsel durumu sıfırlansın
  return <ProductView key={product.data.id} product={product.data} />
}

function ProductView({ product }: { product: ProductDetail }) {
  const [imageIndex, setImageIndex] = useState(0)
  const [variant, setVariant] = useState<Variant | undefined>(
    () => product.variants.find((v) => v.availableQuantity > 0) ?? product.variants[0],
  )
  const [quantity, setQuantity] = useState(1)
  const addToCart = useAddToCart()

  const colors = [...new Set(product.variants.map((v) => v.color).filter(Boolean))] as string[]
  const sizes = [...new Set(product.variants.filter((v) => !variant?.color || v.color === variant.color).map((v) => v.size).filter(Boolean))] as string[]

  function select(color: string | null | undefined, size: string | null | undefined) {
    const match =
      product.variants.find((v) => v.color === (color ?? null) && v.size === (size ?? null)) ??
      product.variants.find((v) => v.color === (color ?? null))
    if (match) {
      setVariant(match)
      setQuantity(1)
    }
  }

  const discount = variant ? discountPercent(variant.price, variant.listPrice) : null
  const outOfStock = !variant || variant.availableQuantity === 0

  return (
    <article className="flex flex-col gap-8">
      <nav aria-label="Konum" className="text-sm text-muted">
        <Link to="/" className="hover:text-ink">Ana sayfa</Link>
        {product.breadcrumbs.map((b) => (
          <span key={b.slug}>
            {' › '}
            <Link to={`/kategori/${b.slug}`} className="hover:text-ink">{b.name}</Link>
          </span>
        ))}
      </nav>

      <div className="grid gap-8 md:grid-cols-2">
        <div className="flex flex-col gap-3">
          <div className="aspect-square overflow-hidden rounded-xl border border-line bg-white">
            {product.images[imageIndex] && <img src={product.images[imageIndex]} alt={product.name} className="h-full w-full object-cover" />}
          </div>
          {product.images.length > 1 && (
            <div className="flex gap-2">
              {product.images.map((src, i) => (
                <button
                  key={src}
                  onClick={() => setImageIndex(i)}
                  aria-label={`Görsel ${i + 1}`}
                  aria-current={i === imageIndex}
                  className={`h-16 w-16 overflow-hidden rounded-lg border-2 ${i === imageIndex ? 'border-brand-600' : 'border-transparent'}`}
                >
                  <img src={src} alt="" className="h-full w-full object-cover" />
                </button>
              ))}
            </div>
          )}
        </div>

        <div className="flex flex-col gap-5">
          <div>
            {product.brandName && <p className="text-sm font-semibold text-muted uppercase">{product.brandName}</p>}
            <h1 className="text-2xl font-bold leading-tight">{product.name}</h1>
            <div className="mt-2 flex items-center gap-3 text-sm">
              {product.reviewCount > 0 && <Rating value={product.rating} count={product.reviewCount} />}
              <span className="text-muted">Satıcı: <strong className="text-ink">{product.seller.storeName}</strong></span>
            </div>
          </div>

          {variant && (
            <div className="flex items-baseline gap-3">
              <span className="text-3xl font-extrabold">{formatPrice(variant.price)}</span>
              {discount && (
                <>
                  <span className="text-muted line-through">{formatPrice(variant.listPrice!)}</span>
                  <span className="rounded-md bg-accent-500 px-1.5 py-0.5 text-xs font-bold text-white">%{discount}</span>
                </>
              )}
            </div>
          )}

          {colors.length > 0 && (
            <fieldset>
              <legend className="mb-2 text-sm font-semibold">Renk: <span className="font-normal">{variant?.color}</span></legend>
              <div className="flex flex-wrap gap-2">
                {colors.map((c) => (
                  <OptionButton key={c} label={c} selected={variant?.color === c} onClick={() => select(c, variant?.size)} />
                ))}
              </div>
            </fieldset>
          )}

          {sizes.length > 0 && (
            <fieldset>
              <legend className="mb-2 text-sm font-semibold">Seçenek</legend>
              <div className="flex flex-wrap gap-2">
                {sizes.map((s) => {
                  const v = product.variants.find((x) => x.size === s && (!variant?.color || x.color === variant.color))
                  return (
                    <OptionButton key={s} label={s} selected={variant?.size === s} disabled={!v || v.availableQuantity === 0} onClick={() => select(variant?.color, s)} />
                  )
                })}
              </div>
            </fieldset>
          )}

          <div className="flex items-center gap-3">
            <label className="sr-only" htmlFor="qty">Adet</label>
            <select
              id="qty"
              value={quantity}
              onChange={(e) => setQuantity(Number(e.target.value))}
              disabled={outOfStock}
              className="rounded-lg border border-line bg-white px-3 py-3"
            >
              {Array.from({ length: Math.min(10, Math.max(1, variant?.availableQuantity ?? 1)) }, (_, i) => i + 1).map((n) => (
                <option key={n} value={n}>{n}</option>
              ))}
            </select>
            <button
              disabled={outOfStock || addToCart.isPending}
              onClick={() => variant && addToCart.mutate({ variantId: variant.id, quantity })}
              className="flex-1 rounded-lg bg-brand-700 px-6 py-3 font-semibold text-white hover:bg-brand-800 disabled:bg-muted"
            >
              {outOfStock ? 'Tükendi' : addToCart.isPending ? 'Ekleniyor…' : 'Sepete ekle'}
            </button>
          </div>
          {addToCart.isSuccess && (
            <p className="text-sm text-brand-700" role="status">
              Sepete eklendi. <Link to="/sepet" className="font-semibold underline">Sepete git</Link>
            </p>
          )}
          {addToCart.isError && (
            <p className="text-sm text-accent-500" role="alert">
              {addToCart.error instanceof ApiError ? addToCart.error.message : 'Sepete eklenemedi.'}
            </p>
          )}
          {variant && variant.availableQuantity > 0 && variant.availableQuantity <= 5 && (
            <p className="text-sm font-medium text-accent-500">Son {variant.availableQuantity} ürün!</p>
          )}

          {product.attributes.length > 0 && (
            <dl className="grid grid-cols-2 gap-x-4 gap-y-2 rounded-xl border border-line bg-white p-4 text-sm">
              {product.attributes.map((a) => (
                <div key={a.name} className="contents">
                  <dt className="text-muted">{a.name}</dt>
                  <dd>{a.value}{a.unit ? ` ${a.unit}` : ''}</dd>
                </div>
              ))}
            </dl>
          )}
        </div>
      </div>

      {product.description && (
        <section aria-labelledby="desc-title" className="rounded-xl border border-line bg-white p-6">
          <h2 id="desc-title" className="mb-3 text-lg font-bold">Ürün açıklaması</h2>
          {/* Açıklama düz metin olarak gösterilir; satıcı HTML'i sunucuda temizlenene kadar HTML olarak basılmaz. */}
          <p className="whitespace-pre-line text-sm leading-relaxed text-muted">{product.description.replace(/<[^>]+>/g, ' ').replace(/\s+/g, ' ').trim()}</p>
        </section>
      )}
    </article>
  )
}

function OptionButton({ label, selected, disabled, onClick }: { label: string; selected: boolean; disabled?: boolean; onClick: () => void }) {
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      aria-pressed={selected}
      className={`min-w-11 rounded-lg border px-3 py-2 text-sm ${selected ? 'border-brand-700 bg-brand-50 font-semibold text-brand-800' : 'border-line bg-white hover:border-ink'} disabled:cursor-not-allowed disabled:opacity-40 disabled:line-through`}
    >
      {label}
    </button>
  )
}
