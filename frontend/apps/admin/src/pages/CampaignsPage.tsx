import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { adminApi } from '../api/admin'
import { CouponType, type AdminCoupon, type CreateCouponInput } from '../api/adminTypes'
import { ApiError } from '../api/client'
import { ErrorState, InlineError, LoadingBlock } from '../components/ErrorState'
import { Field, SelectField } from '../components/Field'
import { MetaBadge } from '../components/StatusBadge'
import { rowClass, RowTh, TableShell, tdClass, Th } from '../components/Table'
import { addDays, formatDateTime, formatDay, formatInclusiveEndDay, todayInIstanbul } from '../lib/dates'
import { formatPrice } from '../lib/format'
import { campaignActionLabel, couponStateMeta, couponTypeLabel } from '../lib/statuses'

export function CampaignsPage() {
  const queryClient = useQueryClient()
  const [creating, setCreating] = useState(false)

  const coupons = useQuery({ queryKey: ['admin', 'coupons'], queryFn: ({ signal }) => adminApi.coupons(signal) })
  const refresh = () => Promise.all([
    queryClient.invalidateQueries({ queryKey: ['admin', 'coupons'] }),
    queryClient.invalidateQueries({ queryKey: ['admin', 'campaign-history'] }),
  ])
  const toggle = useMutation({
    mutationFn: ({ id, active }: { id: number; active: boolean }) => adminApi.setCouponActive(id, active),
    onSuccess: refresh,
  })

  return (
    <section aria-labelledby="campaigns-title" className="flex flex-col gap-8">
      <div className="flex flex-col gap-4">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <h1 id="campaigns-title" className="text-xl font-bold">Kampanyalar</h1>
          {!creating && (
            <button type="button" onClick={() => setCreating(true)} className="rounded-lg bg-brand-700 px-4 py-2 text-sm font-semibold text-white hover:bg-brand-800">Yeni kupon</button>
          )}
        </div>

        {creating && <CouponForm onClose={() => setCreating(false)} onCreated={refresh} />}

        {toggle.isError && <InlineError error={toggle.error} />}

        {coupons.isPending ? (
          <LoadingBlock className="h-48" />
        ) : coupons.isError ? (
          <ErrorState error={coupons.error} onRetry={() => void coupons.refetch()} />
        ) : coupons.data.length === 0 ? (
          <p className="rounded-xl border border-line bg-white px-6 py-16 text-center font-semibold">Henüz kupon yok.</p>
        ) : (
          <div aria-busy={coupons.isFetching}>
            <p className="mb-2 text-xs text-muted">Tarihler İstanbul saatiyle gösterilir; bitiş günü dahildir.</p>
            <TableShell caption="Kuponlar">
              <thead>
                <tr>
                  <Th>Kod</Th><Th>Tip</Th><Th className="text-right">Değer</Th><Th className="text-right">Kullanım</Th><Th>Kapsam</Th>
                  <Th>Başlangıç</Th><Th>Bitiş</Th><Th>Durum</Th><Th>İşlem</Th>
                </tr>
              </thead>
              <tbody>
                {coupons.data.map((c) => (
                  <tr key={c.id} className={rowClass}>
                    <RowTh>
                      {c.code}
                      <span className="block max-w-56 whitespace-normal text-xs font-normal text-muted">{c.description}</span>
                    </RowTh>
                    <td className={tdClass}>{couponTypeLabel[c.type]}</td>
                    <td className={`${tdClass} whitespace-nowrap text-right`}>{couponValue(c)}</td>
                    <td className={`${tdClass} whitespace-nowrap text-right`}>
                      {c.usedCount} / {c.maxUses ?? '∞'}
                      {c.maxUsesPerUser && <span className="block text-xs text-muted">kişi başı {c.maxUsesPerUser}</span>}
                    </td>
                    <td className={tdClass}>{c.sellerName ?? 'Tüm mağazalar'}</td>
                    <td className={`${tdClass} whitespace-nowrap`}>{formatDay(c.startsAt)}</td>
                    <td className={`${tdClass} whitespace-nowrap`}>{formatInclusiveEndDay(c.endsAt)}</td>
                    <td className={tdClass}><MetaBadge value={c.state} meta={couponStateMeta} /></td>
                    <td className={tdClass}>
                      <button
                        type="button"
                        disabled={toggle.isPending && toggle.variables?.id === c.id}
                        onClick={() => toggle.mutate({ id: c.id, active: !c.isActive })}
                        aria-label={`${c.code} kuponunu ${c.isActive ? 'pasifleştir' : 'etkinleştir'}`}
                        className="rounded-lg border border-line px-3 py-1.5 text-xs font-semibold hover:bg-surface disabled:opacity-60"
                      >
                        {c.isActive ? 'Pasifleştir' : 'Etkinleştir'}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </TableShell>
          </div>
        )}
      </div>

      <CampaignHistory />
    </section>
  )
}

function couponValue(c: AdminCoupon) {
  if (c.type === CouponType.Percentage) {
    return (
      <>
        %{c.value.toLocaleString('tr-TR')}
        {c.maxDiscountAmount !== null && <span className="block text-xs text-muted">en fazla {formatPrice(c.maxDiscountAmount)}</span>}
      </>
    )
  }
  if (c.type === CouponType.FixedAmount) return formatPrice(c.value)
  return <span className="text-muted">—</span>
}

const optionalNumber = (data: FormData, key: string) => {
  const raw = String(data.get(key) ?? '').trim()
  return raw === '' ? null : Number(raw)
}

function CouponForm({ onClose, onCreated }: { onClose: () => void; onCreated: () => Promise<unknown> }) {
  const [type, setType] = useState<CouponType>(CouponType.Percentage)
  const today = todayInIstanbul()
  const create = useMutation({
    mutationFn: (input: CreateCouponInput) => adminApi.createCoupon(input),
    onSuccess: async () => {
      await onCreated()
      onClose()
    },
  })

  const errors = create.error instanceof ApiError ? create.error.fieldErrors : {}
  const generalError = create.isError && Object.keys(errors).length === 0 ? create.error : null

  function onSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    const data = new FormData(e.currentTarget)
    create.mutate({
      code: String(data.get('code') ?? '').trim().toUpperCase(),
      description: String(data.get('description') ?? '').trim(),
      type,
      // Ücretsiz kargoda değer kullanılmaz ama backend 0'dan büyük bir değer ister.
      value: type === CouponType.FreeShipping ? 1 : Number(data.get('value')),
      maxDiscountAmount: type === CouponType.Percentage ? optionalNumber(data, 'maxDiscountAmount') : null,
      minOrderAmount: optionalNumber(data, 'minOrderAmount') ?? 0,
      maxUses: optionalNumber(data, 'maxUses'),
      maxUsesPerUser: optionalNumber(data, 'maxUsesPerUser'),
      sellerId: optionalNumber(data, 'sellerId'),
      startsOn: String(data.get('startsOn') ?? ''),
      endsOn: String(data.get('endsOn') ?? ''),
    })
  }

  return (
    <form onSubmit={onSubmit} noValidate aria-busy={create.isPending} aria-labelledby="new-coupon-title" className="flex flex-col gap-4 rounded-xl border border-line bg-white p-4">
      <h2 id="new-coupon-title" className="font-semibold">Yeni kupon</h2>
      {generalError && <InlineError error={generalError} />}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        <Field
          label="Kod"
          name="code"
          autoComplete="off"
          maxLength={20}
          required
          error={errors.code}
          hint="4-20 karakter, büyük harf ve rakam."
          onChange={(e) => { e.target.value = e.target.value.toUpperCase() }}
        />
        <div className="sm:col-span-2">
          <Field label="Açıklama" name="description" maxLength={200} required error={errors.description} />
        </div>
        <SelectField label="Tip" name="type" value={type} onChange={(e) => setType(Number(e.target.value) as CouponType)} error={errors.type}>
          {Object.values(CouponType).map((t) => <option key={t} value={t}>{couponTypeLabel[t]}</option>)}
        </SelectField>
        {type !== CouponType.FreeShipping && (
          <Field
            label={type === CouponType.Percentage ? 'İndirim oranı (%)' : 'İndirim tutarı (TL)'}
            name="value"
            type="number"
            inputMode="decimal"
            min={0.01}
            max={type === CouponType.Percentage ? 90 : undefined}
            step="any"
            required
            error={errors.value}
          />
        )}
        {type === CouponType.Percentage && (
          <Field label="Azami indirim (TL)" name="maxDiscountAmount" type="number" inputMode="decimal" min={0} step="any" error={errors.maxDiscountAmount} hint="Boş bırakılırsa sınırsız." />
        )}
        <Field label="Asgari sepet tutarı (TL)" name="minOrderAmount" type="number" inputMode="decimal" min={0} step="any" defaultValue="0" error={errors.minOrderAmount} />
        <Field label="Toplam kullanım limiti" name="maxUses" type="number" inputMode="numeric" min={1} step={1} error={errors.maxUses} hint="Boş bırakılırsa sınırsız." />
        <Field label="Kişi başı limit" name="maxUsesPerUser" type="number" inputMode="numeric" min={1} max={100} step={1} error={errors.maxUsesPerUser} hint="Boş bırakılırsa sınırsız." />
        <Field label="Satıcı no" name="sellerId" type="number" inputMode="numeric" min={1} step={1} error={errors.sellerId} hint="Boş bırakılırsa tüm mağazalarda geçerli." />
        <Field label="Başlangıç günü" name="startsOn" type="date" defaultValue={today} required error={errors.startsOn} />
        <Field label="Bitiş günü (dahil)" name="endsOn" type="date" defaultValue={addDays(today, 30)} required error={errors.endsOn} />
      </div>
      <div className="flex gap-2">
        <button disabled={create.isPending} className="rounded-lg bg-brand-700 px-4 py-2 text-sm font-semibold text-white hover:bg-brand-800 disabled:opacity-60">
          {create.isPending ? 'Kaydediliyor…' : 'Kuponu oluştur'}
        </button>
        <button type="button" onClick={onClose} className="rounded-lg border border-line px-4 py-2 text-sm hover:bg-surface">Vazgeç</button>
      </div>
    </form>
  )
}

function CampaignHistory() {
  const history = useQuery({ queryKey: ['admin', 'campaign-history', 50], queryFn: ({ signal }) => adminApi.campaignHistory(50, signal) })

  return (
    <section aria-labelledby="history-title" className="flex flex-col gap-3">
      <h2 id="history-title" className="text-lg font-bold">Kampanya geçmişi</h2>
      {history.isPending ? (
        <LoadingBlock className="h-32" />
      ) : history.isError ? (
        <ErrorState error={history.error} onRetry={() => void history.refetch()} />
      ) : history.data.length === 0 ? (
        <p className="rounded-xl border border-line bg-white px-6 py-10 text-center text-sm text-muted">Henüz kampanya hareketi yok.</p>
      ) : (
        <TableShell caption="Kampanya geçmişi">
          <thead>
            <tr><Th>Tarih</Th><Th>İşlem</Th><Th>Kupon</Th><Th>Yapan</Th><Th>Ayrıntı</Th></tr>
          </thead>
          <tbody>
            {history.data.map((h, i) => (
              <tr key={`${h.at}-${i}`} className={rowClass}>
                <RowTh className="whitespace-nowrap font-medium">{formatDateTime(h.at)}</RowTh>
                <td className={tdClass}>{campaignActionLabel(h.action)}</td>
                <td className={tdClass}>{h.couponCode ?? '—'}</td>
                <td className={tdClass}>{h.actorEmail ?? '—'}</td>
                <td className={tdClass}>
                  {h.data ? <div className="max-w-xs whitespace-normal break-all font-mono text-xs text-muted">{h.data}</div> : '—'}
                </td>
              </tr>
            ))}
          </tbody>
        </TableShell>
      )}
    </section>
  )
}
