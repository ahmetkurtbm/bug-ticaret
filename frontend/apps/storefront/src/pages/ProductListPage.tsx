import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { Link, useParams, useSearchParams } from 'react-router'
import { catalogApi } from '../api/catalog'
import type { CategoryNode, ProductSort } from '../api/types'
import { ErrorState } from '../components/ErrorState'
import { Pagination } from '../components/Pagination'
import { ProductGrid, ProductGridSkeleton } from '../components/ProductGrid'

const SORTS: { value: ProductSort; label: string }[] = [
  { value: 'popular', label: 'Çok satanlar' },
  { value: 'price_asc', label: 'En düşük fiyat' },
  { value: 'price_desc', label: 'En yüksek fiyat' },
  { value: 'newest', label: 'En yeniler' },
  { value: 'rating', label: 'En çok beğenilenler' },
]

function findCategory(nodes: CategoryNode[] | undefined, slug: string | undefined): CategoryNode | undefined {
  for (const node of nodes ?? []) {
    if (node.slug === slug) return node
    const child = findCategory(node.children, slug)
    if (child) return child
  }
  return undefined
}

/** Kategori sayfası (/kategori/:slug) ve arama sonuçları (/ara?q=) aynı bileşeni kullanır. */
export function ProductListPage() {
  const { slug } = useParams()
  const [params, setParams] = useSearchParams()

  const q = params.get('q') ?? undefined
  const sort = (params.get('sort') as ProductSort | null) ?? 'popular'
  const page = Number(params.get('page') ?? 1)
  const minPrice = params.get('min') ? Number(params.get('min')) : undefined
  const maxPrice = params.get('max') ? Number(params.get('max')) : undefined

  const categories = useQuery({ queryKey: ['categories'], queryFn: ({ signal }) => catalogApi.categories(signal), staleTime: 10 * 60_000 })
  const category = findCategory(categories.data, slug)

  const search = { category: slug, q, sort, page, minPrice, maxPrice, pageSize: 24 }
  const products = useQuery({
    queryKey: ['products', search],
    queryFn: ({ signal }) => catalogApi.search(search, signal),
    placeholderData: keepPreviousData,
  })

  function update(changes: Record<string, string | undefined>) {
    const next = new URLSearchParams(params)
    for (const [key, value] of Object.entries(changes)) {
      if (value) next.set(key, value)
      else next.delete(key)
    }
    if (!('page' in changes)) next.delete('page')
    setParams(next)
    window.scrollTo({ top: 0 })
  }

  const title = q ? `"${q}" için sonuçlar` : (category?.name ?? 'Ürünler')

  return (
    <div className="grid gap-6 lg:grid-cols-[14rem_1fr]">
      <aside className="flex flex-col gap-6">
        {category && category.children.length > 0 && (
          <section>
            <h2 className="mb-2 text-sm font-semibold">Alt kategoriler</h2>
            <ul className="flex flex-col gap-1 text-sm">
              {category.children.map((c) => (
                <li key={c.id}>
                  <Link to={`/kategori/${c.slug}`} className="text-muted hover:text-brand-700">{c.name}</Link>
                </li>
              ))}
            </ul>
          </section>
        )}
        <section>
          <h2 className="mb-2 text-sm font-semibold">Fiyat aralığı</h2>
          <form
            className="flex items-center gap-2"
            onSubmit={(e) => {
              e.preventDefault()
              const data = new FormData(e.currentTarget)
              update({ min: String(data.get('min') || '') || undefined, max: String(data.get('max') || '') || undefined })
            }}
          >
            <label className="sr-only" htmlFor="min">En az</label>
            <input id="min" name="min" type="number" min={0} defaultValue={minPrice} placeholder="En az" className="w-full rounded-md border border-line px-2 py-1.5 text-sm" />
            <span className="text-muted">–</span>
            <label className="sr-only" htmlFor="max">En çok</label>
            <input id="max" name="max" type="number" min={0} defaultValue={maxPrice} placeholder="En çok" className="w-full rounded-md border border-line px-2 py-1.5 text-sm" />
            <button className="rounded-md bg-brand-700 px-3 py-1.5 text-sm font-semibold text-white">Uygula</button>
          </form>
        </section>
      </aside>

      <section aria-labelledby="list-title" className="min-w-0">
        <div className="mb-4 flex flex-wrap items-end justify-between gap-3">
          <div>
            <h1 id="list-title" className="text-xl font-bold">{title}</h1>
            {products.data && <p className="text-sm text-muted">{products.data.totalCount.toLocaleString('tr-TR')} ürün</p>}
          </div>
          <label className="flex items-center gap-2 text-sm">
            Sırala
            <select value={sort} onChange={(e) => update({ sort: e.target.value })} className="rounded-md border border-line bg-white px-2 py-1.5">
              {SORTS.map((s) => (
                <option key={s.value} value={s.value}>{s.label}</option>
              ))}
            </select>
          </label>
        </div>

        {products.isPending ? (
          <ProductGridSkeleton count={12} />
        ) : products.isError ? (
          <ErrorState error={products.error} onRetry={() => void products.refetch()} />
        ) : products.data.items.length === 0 ? (
          <p className="rounded-xl border border-line bg-white px-6 py-12 text-center text-muted">Aramanızla eşleşen ürün bulunamadı.</p>
        ) : (
          <>
            <div className={products.isPlaceholderData ? 'opacity-60 transition-opacity' : ''}>
              <ProductGrid products={products.data.items} />
            </div>
            <Pagination page={page} totalPages={products.data.totalPages} onChange={(p) => update({ page: String(p) })} />
          </>
        )}
      </section>
    </div>
  )
}
