import { useState, type FormEvent } from 'react'
import { Link, useLocation, useNavigate } from 'react-router'
import { ApiError } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { AuthCard, Field } from './authUi'

export function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [error, setError] = useState<string | null>(null)
  const [pending, setPending] = useState(false)

  async function onSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    const data = new FormData(e.currentTarget)
    setPending(true)
    setError(null)
    try {
      await login(String(data.get('email')), String(data.get('password')))
      navigate((location.state as { from?: string } | null)?.from ?? '/', { replace: true })
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Giriş yapılamadı.')
    } finally {
      setPending(false)
    }
  }

  return (
    <AuthCard title="Giriş yap" footer={<>Hesabınız yok mu? <Link to="/kayit" className="font-semibold text-brand-700 underline">Üye olun</Link></>}>
      <form onSubmit={onSubmit} className="flex flex-col gap-4" noValidate>
        {error && <p role="alert" className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <Field label="E-posta" name="email" type="email" autoComplete="email" />
        <Field label="Şifre" name="password" type="password" autoComplete="current-password" />
        <button disabled={pending} className="rounded-lg bg-brand-700 py-3 font-semibold text-white hover:bg-brand-800 disabled:opacity-60">
          {pending ? 'Giriş yapılıyor…' : 'Giriş yap'}
        </button>
        <p className="text-xs text-muted">Demo hesap: musteri@tezgah.test / Tezgah123!</p>
      </form>
    </AuthCard>
  )
}
