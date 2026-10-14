import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router'
import { catalogApi } from '../api/catalog'
import { ErrorState } from '../components/ErrorState'
import { ProductGrid, ProductGridSkeleton } from '../components/ProductGrid'

export function HomePage() {
  const popular = useQuery({
    queryKey: ['products', { sort: 'popular', pageSize: 20 }],
    queryFn: ({ signal }) => catalogApi.search({ sort: 'popular', pageSize: 20 }, signal),
  })
  const categories = useQuery({ queryKey: ['categories'], queryFn: ({ signal }) => catalogApi.categories(signal), staleTime: 10 * 60_000 })

  return (
    <div className="flex flex-col gap-10">
      <section className="rounded-2xl bg-brand-700 px-6 py-10 text-white sm:px-10">
        <h1 className="max-w-xl text-3xl font-extrabold tracking-tight sm:text-4xl">Binlerce satıcı, tek sepet.</h1>
        <p className="mt-3 max-w-lg text-brand-100">750 TL üzeri alışverişte 100 TL indirim için sepette <strong>SEPET100</strong> kodunu kullanın.</p>
        <Link to="/kategori/elektronik" className="mt-6 inline-block rounded-lg bg-white px-5 py-2.5 text-sm font-semibold text-brand-800 hover:bg-brand-50">
          Elektronikte fırsatlar
        </Link>
      </section>

      {categories.data && (
        <section aria-labelledby="cat-title">
          <h2 id="cat-title" className="mb-3 text-lg font-bold">Kategoriler</h2>
          <ul className="flex flex-wrap gap-2">
            {categories.data.flatMap((root) => root.children.length ? root.children : [root]).slice(0, 18).map((c) => (
              <li key={c.id}>
                <Link to={`/kategori/${c.slug}`} className="block rounded-full border border-line bg-white px-4 py-2 text-sm hover:border-brand-600 hover:text-brand-700">
                  {c.name}
                </Link>
              </li>
            ))}
          </ul>
        </section>
      )}

      <section aria-labelledby="popular-title">
        <h2 id="popular-title" className="mb-3 text-lg font-bold">Çok satanlar</h2>
        {popular.isPending ? (
          <ProductGridSkeleton />
        ) : popular.isError ? (
          <ErrorState error={popular.error} onRetry={() => void popular.refetch()} />
        ) : (
          <ProductGrid products={popular.data.items} />
        )}
      </section>
    </div>
  )
}
