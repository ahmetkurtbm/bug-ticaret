import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { useSearchParams } from 'react-router'
import { adminApi } from '../api/admin'
import { SellerStatus, type AdminSeller } from '../api/adminTypes'
import { ApiError } from '../api/client'
import { ErrorState, InlineError, LoadingBlock } from '../components/ErrorState'
import { Pagination } from '../components/Pagination'
import { MetaBadge } from '../components/StatusBadge'
import { rowClass, RowTh, TableShell, tdClass, Th } from '../components/Table'
import { formatDay } from '../lib/dates'
import { sellerStatusMeta } from '../lib/statuses'

const tabs = [
  { key: '1', label: 'Bekleyen', status: SellerStatus.Pending },
  { key: '2', label: 'Aktif', status: SellerStatus.Active },
  { key: '3', label: 'Askıda', status: SellerStatus.Suspended },
  { key: '4', label: 'Kapalı', status: SellerStatus.Closed },
  { key: 'tumu', label: 'Tümü', status: null },
]

export function SellersPage() {
  const [params, setParams] = useSearchParams()
  const tab = tabs.find((t) => t.key === params.get('durum')) ?? tabs[0]
  const q = params.get('q') ?? ''
  const page = Math.max(1, Number(params.get('sayfa')) || 1)

  const sellers = useQuery({
    queryKey: ['admin', 'sellers', { status: tab.status, q, page }],
    queryFn: ({ signal }) => adminApi.sellers({ status: tab.status, q: q.trim(), page }, signal),
    placeholderData: keepPreviousData,
  })

  const update = (changes: Record<string, string | null>) => {
    const next = new URLSearchParams(params)
    for (const [key, value] of Object.entries(changes)) {
      if (value) next.set(key, value)
      else next.delete(key)
    }
    setParams(next)
  }

  return (
    <section aria-labelledby="sellers-title" className="flex flex-col gap-4">
      <h1 id="sellers-title" className="text-xl font-bold">Satıcılar</h1>

      <div className="flex flex-wrap items-end justify-between gap-3">
        <div className="flex flex-wrap gap-2" role="group" aria-label="Satıcı durumu">
          {tabs.map((t) => (
            <button
              key={t.key}
              type="button"
              aria-pressed={t.key === tab.key}
              onClick={() => update({ durum: t.key === '1' ? null : t.key, sayfa: null })}
              className={`rounded-lg border px-3 py-2 text-sm ${t.key === tab.key ? 'border-brand-700 bg-brand-700 font-semibold text-white' : 'border-line bg-white hover:bg-surface'}`}
            >
              {t.label}
            </button>
          ))}
        </div>
        <form
          key={q}
          role="search"
          aria-label="Satıcı ara"
          onSubmit={(e) => {
            e.preventDefault()
            update({ q: String(new FormData(e.currentTarget).get('q') ?? '').trim() || null, sayfa: null })
          }}
          className="flex items-end gap-2"
        >
          <div className="flex flex-col gap-1.5">
            <label htmlFor="seller-q" className="text-sm font-medium">Ara</label>
            <input id="seller-q" name="q" type="search" defaultValue={q} placeholder="Mağaza, ünvan veya e-posta" className="w-56 max-w-full rounded-lg border border-line bg-white px-3 py-2 text-sm" />
          </div>
          <button className="rounded-lg border border-line px-3 py-2 text-sm hover:bg-surface">Ara</button>
        </form>
      </div>

      {sellers.isPending ? (
        <LoadingBlock />
      ) : sellers.isError ? (
        <ErrorState error={sellers.error} onRetry={() => void sellers.refetch()} />
      ) : sellers.data.items.length === 0 ? (
        <p className="rounded-xl border border-line bg-white px-6 py-16 text-center font-semibold">Bu görünümde satıcı yok.</p>
      ) : (
        <div aria-busy={sellers.isFetching}>
          <TableShell caption="Satıcılar">
            <thead>
              <tr>
                <Th>Mağaza</Th><Th>Sahip</Th><Th>Şehir</Th><Th className="text-right">Ürün</Th><Th>Komisyon</Th><Th>Durum</Th><Th>Başvuru</Th><Th>İşlemler</Th>
              </tr>
            </thead>
            <tbody>
              {sellers.data.items.map((s) => <SellerRow key={s.id} seller={s} />)}
            </tbody>
          </TableShell>
          <Pagination page={sellers.data.page} totalPages={sellers.data.totalPages} onChange={(p) => update({ sayfa: String(p) })} />
        </div>
      )}
    </section>
  )
}

type Mode = 'none' | 'decision' | 'commission'

function SellerRow({ seller }: { seller: AdminSeller }) {
  const queryClient = useQueryClient()
  const [mode, setMode] = useState<Mode>('none')
  const [note, setNote] = useState('')
  const [noteError, setNoteError] = useState<string | null>(null)
  const [rate, setRate] = useState(String(seller.commissionRate))

  const refresh = () => queryClient.invalidateQueries({ queryKey: ['admin', 'sellers'] })
  const approve = useMutation({ mutationFn: () => adminApi.approveSeller(seller.id), onSuccess: refresh })
  const decide = useMutation({
    mutationFn: (text: string) => adminApi.suspendSeller(seller.id, text),
    onSuccess: async () => {
      setMode('none')
      await refresh()
    },
  })
  const commission = useMutation({
    mutationFn: (value: number) => adminApi.updateCommission(seller.id, value),
    onSuccess: async () => {
      setMode('none')
      await refresh()
    },
  })

  const isPending = seller.status === SellerStatus.Pending
  const isActive = seller.status === SellerStatus.Active
  const busy = approve.isPending || decide.isPending || commission.isPending
  const decisionLabel = isPending ? 'Reddet' : 'Askıya al'
  const noteId = `note-${seller.id}`
  const rateId = `rate-${seller.id}`

  function submitDecision(e: FormEvent) {
    e.preventDefault()
    if (!note.trim()) {
      setNoteError('Lütfen bir not girin.')
      return
    }
    setNoteError(null)
    decide.mutate(note.trim())
  }

  function submitCommission(e: FormEvent) {
    e.preventDefault()
    commission.mutate(Number(rate))
  }

  function open(next: Mode) {
    setMode(next)
    setNote('')
    setNoteError(null)
    setRate(String(seller.commissionRate))
    decide.reset()
    commission.reset()
  }

  const commissionError = commission.error instanceof ApiError ? (commission.error.fieldErrors.commissionRate ?? commission.error.message) : undefined

  return (
    <>
      <tr className={mode === 'none' ? rowClass : 'border-b-0'}>
        <RowTh>
          {seller.storeName}
          <span className="block text-xs font-normal text-muted">{seller.legalName} · VKN {seller.taxNumber}</span>
        </RowTh>
        <td className={tdClass}>
          <div>{seller.ownerName}</div>
          <div className="text-xs text-muted">{seller.ownerEmail}</div>
        </td>
        <td className={tdClass}>{seller.city}</td>
        <td className={`${tdClass} text-right`}>{seller.productCount}</td>
        <td className={`${tdClass} whitespace-nowrap`}>
          %{seller.commissionRate.toLocaleString('tr-TR')}
          {seller.status !== SellerStatus.Closed && (
            <button type="button" onClick={() => open('commission')} aria-label={`${seller.storeName} komisyonunu düzenle`} className="ml-2 text-xs font-semibold text-brand-700 underline">
              Düzenle
            </button>
          )}
        </td>
        <td className={tdClass}><MetaBadge value={seller.status} meta={sellerStatusMeta} /></td>
        <td className={`${tdClass} whitespace-nowrap`}>{formatDay(seller.createdAt)}</td>
        <td className={tdClass}>
          <div className="flex flex-wrap gap-2">
            {isPending && (
              <button type="button" disabled={busy} onClick={() => approve.mutate()} className="rounded-lg bg-brand-700 px-3 py-1.5 text-xs font-semibold text-white hover:bg-brand-800 disabled:opacity-60">
                {approve.isPending ? 'Onaylanıyor…' : 'Onayla'}
              </button>
            )}
            {(isPending || isActive) && (
              <button type="button" disabled={busy} onClick={() => open('decision')} className="rounded-lg border border-red-300 px-3 py-1.5 text-xs font-semibold text-red-700 hover:bg-red-50 disabled:opacity-60">
                {decisionLabel}
              </button>
            )}
          </div>
          {approve.isError && <div className="mt-2 max-w-64 whitespace-normal"><InlineError error={approve.error} /></div>}
        </td>
      </tr>
      {mode !== 'none' && (
        <tr className={rowClass}>
          <td colSpan={8} className="bg-surface px-3 py-3">
            {mode === 'decision' ? (
              <form onSubmit={submitDecision} aria-busy={decide.isPending} className="flex max-w-xl flex-col gap-2" noValidate>
                <label htmlFor={noteId} className="text-sm font-medium">{seller.storeName} için {decisionLabel.toLowerCase()} gerekçesi</label>
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
                <InlineError error={decide.error} />
                <div className="flex gap-2">
                  <button disabled={decide.isPending} className="rounded-lg bg-red-700 px-3 py-1.5 text-sm font-semibold text-white hover:bg-red-800 disabled:opacity-60">
                    {decide.isPending ? 'Gönderiliyor…' : `${decisionLabel} — onayla`}
                  </button>
                  <button type="button" onClick={() => setMode('none')} className="rounded-lg border border-line bg-white px-3 py-1.5 text-sm hover:bg-surface">Vazgeç</button>
                </div>
              </form>
            ) : (
              <form onSubmit={submitCommission} aria-busy={commission.isPending} className="flex flex-wrap items-start gap-2" noValidate>
                <div className="flex flex-col gap-1">
                  <label htmlFor={rateId} className="text-sm font-medium">{seller.storeName} komisyon oranı (%)</label>
                  <input
                    id={rateId}
                    type="number"
                    inputMode="decimal"
                    min={0}
                    max={50}
                    step="0.1"
                    value={rate}
                    onChange={(e) => setRate(e.target.value)}
                    aria-invalid={commissionError ? true : undefined}
                    aria-describedby={commissionError ? `${rateId}-err` : undefined}
                    className="w-32 rounded-lg border border-line bg-white px-3 py-2 text-sm aria-invalid:border-red-500"
                  />
                  {commissionError && <p id={`${rateId}-err`} role="alert" className="text-xs text-red-700">{commissionError}</p>}
                  {commission.isError && !(commission.error instanceof ApiError) && <InlineError error={commission.error} />}
                </div>
                <div className="flex gap-2 pt-6">
                  <button disabled={commission.isPending || rate.trim() === ''} className="rounded-lg bg-brand-700 px-3 py-2 text-sm font-semibold text-white hover:bg-brand-800 disabled:opacity-60">
                    {commission.isPending ? 'Kaydediliyor…' : 'Kaydet'}
                  </button>
                  <button type="button" onClick={() => setMode('none')} className="rounded-lg border border-line bg-white px-3 py-2 text-sm hover:bg-surface">Vazgeç</button>
                </div>
              </form>
            )}
          </td>
        </tr>
      )}
    </>
  )
}
