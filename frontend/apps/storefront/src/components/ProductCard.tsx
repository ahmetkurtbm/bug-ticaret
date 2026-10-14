import { Link } from 'react-router'
import type { ProductCard as ProductCardModel } from '../api/types'
import { discountPercent, formatPrice } from '../lib/format'
import { Rating } from './Rating'

export function ProductCard({ product }: { product: ProductCardModel }) {
  const discount = discountPercent(product.price, product.listPrice)
  return (
    <Link
      to={`/urun/${product.slug}`}
      className="group flex flex-col overflow-hidden rounded-xl border border-line bg-white transition-shadow hover:shadow-md"
    >
      <div className="relative aspect-square bg-surface">
        {product.imageUrl && (
          <img src={product.imageUrl} alt="" loading="lazy" className="h-full w-full object-cover" width={600} height={600} />
        )}
        {discount && (
          <span className="absolute top-2 left-2 rounded-md bg-accent-500 px-1.5 py-0.5 text-xs font-bold text-white">%{discount}</span>
        )}
      </div>
      <div className="flex flex-1 flex-col gap-1 p-3">
        {product.brandName && <span className="text-xs font-semibold text-muted uppercase">{product.brandName}</span>}
        <h3 className="line-clamp-2 text-sm leading-snug group-hover:text-brand-700">{product.name}</h3>
        {product.reviewCount > 0 && <Rating value={product.rating} count={product.reviewCount} />}
        <div className="mt-auto flex items-baseline gap-2 pt-1">
          <span className="text-base font-bold">{formatPrice(product.price)}</span>
          {discount && <span className="text-xs text-muted line-through">{formatPrice(product.listPrice!)}</span>}
        </div>
        <span className="text-xs text-muted">{product.sellerName}</span>
      </div>
    </Link>
  )
}
