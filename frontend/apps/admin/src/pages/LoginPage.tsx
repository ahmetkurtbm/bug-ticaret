import { useState, type FormEvent } from 'react'
import { Navigate, useLocation } from 'react-router'
import { ApiError } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { isAdmin } from '../auth/roles'
import { AccessDenied } from '../components/AccessDenied'
import { Field } from '../components/Field'

export function LoginPage() {
  const { user, login } = useAuth()
  const location = useLocation()
  const [error, setError] = useState<string | null>(null)
  const [pending, setPending] = useState(false)

  if (user && isAdmin(user)) return <Navigate to={(location.state as { from?: string } | null)?.from ?? '/'} replace />
  if (user) return <AccessDenied />

  async function onSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    const data = new FormData(e.currentTarget)
    setPending(true)
    setError(null)
    try {
      await login(String(data.get('email')), String(data.get('password')))
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Giriş yapılamadı.')
    } finally {
      setPending(false)
    }
  }

  return (
    <main className="mx-auto flex min-h-screen max-w-md items-center px-4">
      <div className="w-full rounded-2xl border border-line bg-white p-6 sm:p-8">
        <h1 className="mb-1 text-2xl font-bold">Yönetim paneli</h1>
        <p className="mb-6 text-sm text-muted">Devam etmek için yönetici hesabınızla giriş yapın.</p>
        <form onSubmit={onSubmit} className="flex flex-col gap-4" noValidate aria-busy={pending}>
          {error && <p role="alert" className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
          <Field label="E-posta" name="email" type="email" autoComplete="email" required />
          <Field label="Şifre" name="password" type="password" autoComplete="current-password" required />
          <button disabled={pending} className="rounded-lg bg-brand-700 py-3 font-semibold text-white hover:bg-brand-800 disabled:opacity-60">
            {pending ? 'Giriş yapılıyor…' : 'Giriş yap'}
          </button>
        </form>
      </div>
    </main>
  )
}
