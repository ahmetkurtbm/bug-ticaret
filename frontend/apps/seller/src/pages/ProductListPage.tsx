import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { Link, useSearchParams } from 'react-router'
import { sellerApi } from '../api/seller'
import { ProductStatus } from '../api/types'
import { ErrorState, LoadingBlock } from '../components/ErrorState'
import { controlClass, primaryButton, secondaryButton } from '../components/Field'
import { Pagination } from '../components/Pagination'
import { ProductStatusBadge } from '../components/StatusBadge'
import { formatPrice } from '../lib/format'
import { productStatusLabel } from '../lib/status'

const PAGE_SIZE = 20
const statusValues = Object.values(ProductStatus)

export function ProductListPage() {
  const [params, setParams] = useSearchParams()
  const q = params.get('q') ?? ''
  const statusParam = Number(params.get('status'))
  const status = statusValues.find((s) => s === statusParam)
  const page = Math.max(1, Number(params.get('page')) || 1)
  const [search, setSearch] = useState(q)

  const products = useQuery({
    queryKey: ['seller', 'products', { q, status, page }],
    queryFn: ({ signal }) => sellerApi.products({ q: q || undefined, status, page, pageSize: PAGE_SIZE }, signal),
    placeholderData: keepPreviousData,
  })

  function update(changes: Record<string, string | null>) {
    const next = new URLSearchParams(params)
    for (const [key, value] of Object.entries(changes)) {
      if (value) next.set(key, value)
      else next.delete(key)
    }
    setParams(next)
  }

  function onSearch(e: FormEvent) {
    e.preventDefault()
    update({ q: search.trim() || null, page: null })
  }

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-xl font-bold">Ürünler</h1>
        <Link to="/urunler/yeni" className={primaryButton}>Yeni ürün</Link>
      </div>

      <div className="flex flex-col gap-3 sm:flex-row sm:items-end">
        <form onSubmit={onSearch} role="search" className="flex min-w-0 flex-1 gap-2">
          <label htmlFor="product-search" className="sr-only">Ürün ara</label>
          <input id="product-search" type="search" value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Ürün adı ara" className={controlClass} />
          <button className={secondaryButton}>Ara</button>
        </form>
        <div className="flex flex-col gap-1 sm:w-48">
          <label htmlFor="product-status" className="text-sm font-medium">Durum</label>
          <select
            id="product-status"
            value={status ?? ''}
            onChange={(e) => update({ status: e.target.value || null, page: null })}
            className={controlClass}
          >
            <option value="">Tümü</option>
            {statusValues.map((s) => (
              <option key={s} value={s}>{productStatusLabel[s]}</option>
            ))}
          </select>
        </div>
      </div>

      {products.isPending ? (
        <LoadingBlock />
      ) : products.isError ? (
        <ErrorState error={products.error} onRetry={() => void products.refetch()} />
      ) : products.data.items.length === 0 ? (
        <div className="rounded-xl border border-line bg-white px-6 py-14 text-center">
          <p className="font-semibold">Ürün bulunamadı</p>
          <p className="mt-1 text-sm text-muted">{q || status ? 'Filtreleri değiştirmeyi deneyin.' : 'İlk ürününüzü ekleyerek satışa başlayın.'}</p>
        </div>
      ) : (
        <div aria-busy={products.isFetching}>
          <ul className="flex flex-col gap-3">
            {products.data.items.map((p) => (
              <li key={p.id}>
                <Link to={`/urunler/${p.id}`} className="flex items-start gap-3 rounded-xl border border-line bg-white p-3 hover:border-brand-600 sm:items-center sm:gap-4">
                  {p.imageUrl ? (
                    <img src={p.imageUrl} alt="" loading="lazy" className="size-16 shrink-0 rounded-lg bg-surface object-cover" />
                  ) : (
                    <div className="size-16 shrink-0 rounded-lg bg-surface" aria-hidden="true" />
                  )}
                  <div className="grid min-w-0 flex-1 gap-x-4 gap-y-1 sm:grid-cols-[1fr_auto]">
                    <div className="min-w-0">
                      <p className="break-words font-semibold">{p.name}</p>
                      <p className="text-sm text-muted">{p.categoryName}</p>
                    </div>
                    <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-sm sm:justify-end">
                      <ProductStatusBadge status={p.status} />
                      <span>{p.variantCount} varyant</span>
                      <span>Stok: {p.totalStock}</span>
                      <span className="font-semibold">{formatPrice(p.minPrice)}</span>
                    </div>
                  </div>
                </Link>
              </li>
            ))}
          </ul>
          <Pagination page={products.data.page} totalPages={products.data.totalPages} onChange={(p) => update({ page: p > 1 ? String(p) : null })} />
        </div>
      )}
    </div>
  )
}
