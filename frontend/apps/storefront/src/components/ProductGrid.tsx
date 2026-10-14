import type { ProductCard as ProductCardModel } from '../api/types'
import { ProductCard } from './ProductCard'

export function ProductGrid({ products }: { products: ProductCardModel[] }) {
  return (
    <ul className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5">
      {products.map((p) => (
        <li key={p.id}>
          <ProductCard product={p} />
        </li>
      ))}
    </ul>
  )
}

export function ProductGridSkeleton({ count = 10 }: { count?: number }) {
  return (
    <ul className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5" aria-busy="true" aria-label="Yükleniyor">
      {Array.from({ length: count }, (_, i) => (
        <li key={i} className="aspect-[3/4] animate-pulse rounded-xl bg-line/60" />
      ))}
    </ul>
  )
}
