import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { useSearchParams } from 'react-router'
import { adminApi } from '../api/admin'
import { ProductStatus, type AdminProduct } from '../api/adminTypes'
import { ErrorState, InlineError, LoadingBlock } from '../components/ErrorState'
import { Pagination } from '../components/Pagination'
import { rowClass, RowTh, TableShell, tdClass, Th } from '../components/Table'
import { formatDay } from '../lib/dates'
import { formatPrice } from '../lib/format'

export function ProductApprovalPage() {
  const [params, setParams] = useSearchParams()
  const page = Math.max(1, Number(params.get('sayfa')) || 1)

  const products = useQuery({
    queryKey: ['admin', 'products', { status: ProductStatus.PendingApproval, page }],
    queryFn: ({ signal }) => adminApi.products({ status: ProductStatus.PendingApproval, page }, signal),
    placeholderData: keepPreviousData,
  })

  return (
    <section aria-labelledby="products-title" className="flex flex-col gap-4">
      <h1 id="products-title" className="text-xl font-bold">Ürün onayı</h1>

      {products.isPending ? (
        <LoadingBlock />
      ) : products.isError ? (
        <ErrorState error={products.error} onRetry={() => void products.refetch()} />
      ) : products.data.items.length === 0 ? (
        <p className="rounded-xl border border-line bg-white px-6 py-16 text-center font-semibold">Onay bekleyen ürün yok.</p>
      ) : (
        <div aria-busy={products.isFetching}>
          <p className="mb-2 text-sm text-muted">{products.data.totalCount.toLocaleString('tr-TR')} ürün onay bekliyor</p>
          <TableShell caption="Onay bekleyen ürünler">
            <thead>
              <tr>
                <Th><span className="sr-only">Görsel</span></Th><Th>Ürün</Th><Th>Satıcı</Th><Th>Kategori</Th>
                <Th className="text-right">Varyant</Th><Th className="text-right">Fiyat</Th><Th>Tarih</Th><Th>İşlemler</Th>
              </tr>
            </thead>
            <tbody>
              {products.data.items.map((p) => <ProductRow key={p.id} product={p} />)}
            </tbody>
          </TableShell>
          <Pagination page={products.data.page} totalPages={products.data.totalPages} onChange={(p) => setParams({ sayfa: String(p) })} />
        </div>
      )}
    </section>
  )
}

function ProductRow({ product }: { product: AdminProduct }) {
  const queryClient = useQueryClient()
  const [rejecting, setRejecting] = useState(false)
  const [note, setNote] = useState('')
  const [noteError, setNoteError] = useState<string | null>(null)

  const refresh = () => queryClient.invalidateQueries({ queryKey: ['admin', 'products'] })
  const approve = useMutation({ mutationFn: () => adminApi.approveProduct(product.id), onSuccess: refresh })
  const reject = useMutation({ mutationFn: (text: string) => adminApi.rejectProduct(product.id, text), onSuccess: refresh })
  const busy = approve.isPending || reject.isPending
  const noteId = `reject-note-${product.id}`

  function submitReject(e: FormEvent) {
    e.preventDefault()
    if (!note.trim()) {
      setNoteError('Lütfen bir not girin.')
      return
    }
    setNoteError(null)
    reject.mutate(note.trim())
  }

  return (
    <>
      <tr className={rejecting || approve.isError ? 'border-b-0' : rowClass}>
        <td className={tdClass}>
          {product.imageUrl ? (
            <img src={product.imageUrl} alt="" loading="lazy" className="h-14 w-14 rounded-lg border border-line bg-surface object-cover" />
          ) : (
            <div className="h-14 w-14 rounded-lg border border-line bg-surface" aria-hidden="true" />
          )}
        </td>
        <RowTh>{product.name}</RowTh>
        <td className={tdClass}>{product.sellerName}</td>
        <td className={tdClass}>{product.categoryName}</td>
        <td className={`${tdClass} text-right`}>{product.variantCount}</td>
        <td className={`${tdClass} whitespace-nowrap text-right`}>
          {product.variantCount > 1 && <span className="text-xs text-muted">başlangıç </span>}
          {formatPrice(product.minPrice)}
        </td>
        <td className={`${tdClass} whitespace-nowrap`}>{formatDay(product.updatedAt ?? product.createdAt)}</td>
        <td className={tdClass}>
          <div className="flex flex-wrap gap-2">
            <button type="button" disabled={busy} onClick={() => approve.mutate()} aria-label={`${product.name} ürününü onayla`} className="rounded-lg bg-brand-700 px-3 py-1.5 text-xs font-semibold text-white hover:bg-brand-800 disabled:opacity-60">
              {approve.isPending ? 'Onaylanıyor…' : 'Onayla'}
            </button>
            <button type="button" disabled={busy} onClick={() => setRejecting(true)} aria-label={`${product.name} ürününü reddet`} className="rounded-lg border border-red-300 px-3 py-1.5 text-xs font-semibold text-red-700 hover:bg-red-50 disabled:opacity-60">
              Reddet
            </button>
          </div>
        </td>
      </tr>
      {(rejecting || approve.isError) && (
        <tr className={rowClass}>
          <td colSpan={8} className="bg-surface px-3 py-3">
            {approve.isError && <div className="mb-2 max-w-xl"><InlineError error={approve.error} /></div>}
            {rejecting && (
              <form onSubmit={submitReject} aria-busy={reject.isPending} className="flex max-w-xl flex-col gap-2" noValidate>
                <label htmlFor={noteId} className="text-sm font-medium">{product.name} için ret gerekçesi</label>
                <textarea
                  id={noteId}
                  value={note}
                  onChange={(e) => setNote(e.target.value)}
                  maxLength={500}
                  rows={2}
                  aria-invalid={noteError ? true : undefined}
                  aria-describedby={noteError ? `${noteId}-err` : undefined}
                  className="rounded-lg border border-line bg-white px-3 py-2 text-sm aria-invalid:border-red-500"
                />
                {noteError && <p id={`${noteId}-err`} role="alert" className="text-xs text-red-700">{noteError}</p>}
                <InlineError error={reject.error} />
                <div className="flex gap-2">
                  <button disabled={reject.isPending} className="rounded-lg bg-red-700 px-3 py-1.5 text-sm font-semibold text-white hover:bg-red-800 disabled:opacity-60">
                    {reject.isPending ? 'Gönderiliyor…' : 'Reddet — onayla'}
                  </button>
                  <button type="button" onClick={() => setRejecting(false)} className="rounded-lg border border-line bg-white px-3 py-1.5 text-sm hover:bg-surface">Vazgeç</button>
                </div>
              </form>
            )}
          </td>
        </tr>
      )}
    </>
  )
}
