import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent, type ReactNode } from 'react'
import { Link } from 'react-router'
import { ApiError } from '../api/client'
import { sellerApi } from '../api/seller'
import { SellerStatus, type SellerProfile } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import { useSignOut } from '../auth/useSignOut'
import { ErrorState, LoadingBlock } from '../components/ErrorState'
import { Alert, Field, primaryButton } from '../components/Field'
import { hasSellerRole } from '../lib/roles'
import { PlainShell } from './authUi'

const applicationKey = ['seller-application'] as const

export function ApplicationPage() {
  const { user } = useAuth()
  const signOut = useSignOut()
  const application = useQuery({ queryKey: applicationKey, queryFn: ({ signal }) => sellerApi.myApplication(signal) })

  return (
    <PlainShell wide>
      <div className="mb-4 flex items-center justify-between gap-3 text-sm">
        <span className="min-w-0 truncate text-muted">{user?.email}</span>
        <button onClick={() => void signOut()} className="font-semibold text-brand-700 hover:underline">Çıkış yap</button>
      </div>
      {application.isPending ? (
        <LoadingBlock />
      ) : application.isError ? (
        <ErrorState error={application.error} onRetry={() => void application.refetch()} />
      ) : application.data === null ? (
        <ApplicationForm />
      ) : (
        <StatusCard profile={application.data} />
      )}
    </PlainShell>
  )
}

function Card({ title, children }: { title: string; children: ReactNode }) {
  return (
    <section className="rounded-2xl border border-line bg-white p-6 sm:p-8">
      <h1 className="mb-4 text-2xl font-bold">{title}</h1>
      {children}
    </section>
  )
}

function StatusCard({ profile }: { profile: SellerProfile }) {
  const { user } = useAuth()
  const signOut = useSignOut()
  const summary = (
    <p className="mt-4 text-sm text-muted">Mağaza: <span className="font-semibold text-ink">{profile.storeName}</span></p>
  )

  if (profile.status === SellerStatus.Pending) {
    return (
      <Card title="Başvurunuz inceleniyor">
        <p role="status">Başvurunuz alındı. Ekibimiz bilgilerinizi inceliyor; sonuçlandığında bu sayfadan görebilirsiniz.</p>
        {summary}
      </Card>
    )
  }
  if (profile.status === SellerStatus.Closed) {
    return (
      <Card title="Başvurunuz reddedildi">
        <p>Mağaza başvurunuz onaylanmadı. Ayrıntı için destek ekibiyle iletişime geçebilirsiniz.</p>
        {summary}
      </Card>
    )
  }
  if (profile.status === SellerStatus.Suspended) {
    return (
      <Card title="Mağazanız askıya alındı">
        <p>Mağazanız şu an askıda; panele erişim kapalı. Ayrıntı için destek ekibiyle iletişime geçin.</p>
        {summary}
      </Card>
    )
  }
  // Active: rol yeni token'a gelir; eski token'la panel açılmaz.
  return (
    <Card title="Başvurunuz onaylandı">
      {hasSellerRole(user) ? (
        <>
          <p>Mağazanız aktif.</p>
          <Link to="/" className={`${primaryButton} mt-5 inline-block`}>Panele git</Link>
        </>
      ) : (
        <>
          <p role="status">Onaylandı, panele geçmek için yeniden giriş yapın.</p>
          <button onClick={() => void signOut()} className={`${primaryButton} mt-5`}>Çıkış yap ve yeniden giriş yap</button>
        </>
      )}
      {summary}
    </Card>
  )
}

function ApplicationForm() {
  const queryClient = useQueryClient()
  const [values, setValues] = useState({ storeName: '', legalName: '', taxNumber: '', taxOffice: '', city: '', iban: '' })
  const apply = useMutation({
    mutationFn: sellerApi.apply,
    onSuccess: (profile) => queryClient.setQueryData(applicationKey, profile),
  })
  const errors = apply.error instanceof ApiError ? apply.error.fieldErrors : {}
  const generalError = apply.error && Object.keys(errors).length === 0 ? (apply.error instanceof ApiError ? apply.error.message : 'Başvuru gönderilemedi.') : null
  const set = (key: keyof typeof values) => (e: { target: { value: string } }) => setValues((v) => ({ ...v, [key]: e.target.value }))

  function onSubmit(e: FormEvent) {
    e.preventDefault()
    if (apply.isPending) return
    // IBAN olduğu gibi gider (boşluklu yazılabilir); sunucu temizler.
    apply.mutate({
      storeName: values.storeName.trim(),
      legalName: values.legalName.trim(),
      taxNumber: values.taxNumber.trim(),
      taxOffice: values.taxOffice.trim(),
      city: values.city.trim(),
      iban: values.iban,
    })
  }

  return (
    <Card title="Satıcı başvurusu">
      <p className="mb-5 text-sm text-muted">Tezgah&apos;ta satış yapmak için mağaza ve vergi bilgilerinizi girin.</p>
      <form onSubmit={onSubmit} className="flex flex-col gap-4" noValidate aria-busy={apply.isPending}>
        {generalError && <Alert>{generalError}</Alert>}
        <Field label="Mağaza adı" name="storeName" value={values.storeName} onChange={set('storeName')} error={errors.storeName} />
        <Field label="Ticari unvan / ad soyad" name="legalName" value={values.legalName} onChange={set('legalName')} error={errors.legalName} />
        <div className="grid gap-4 sm:grid-cols-2">
          <Field label="Vergi no / T.C. kimlik no" name="taxNumber" value={values.taxNumber} onChange={set('taxNumber')} error={errors.taxNumber} inputMode="numeric" hint="10 haneli vergi no ya da 11 haneli T.C. kimlik no" />
          <Field label="Vergi dairesi" name="taxOffice" value={values.taxOffice} onChange={set('taxOffice')} error={errors.taxOffice} />
        </div>
        <Field label="IBAN" name="iban" value={values.iban} onChange={set('iban')} error={errors.iban} autoComplete="off" placeholder="TR00 0000 0000 0000 0000 0000 00" hint="Boşluklu yazabilirsiniz." />
        <Field label="Şehir" name="city" value={values.city} onChange={set('city')} error={errors.city} autoComplete="address-level1" />
        <button disabled={apply.isPending} className={`${primaryButton} py-3`}>{apply.isPending ? 'Gönderiliyor…' : 'Başvuruyu gönder'}</button>
      </form>
    </Card>
  )
}
