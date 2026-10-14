import { useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { Link, useLocation, useNavigate } from 'react-router'
import { ApiError } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { tokenStore } from '../auth/tokenStore'
import { Alert, Field, primaryButton } from '../components/Field'
import { hasSellerRole } from '../lib/roles'
import { AuthCard } from './authUi'

export function LoginPage() {
  const { login } = useAuth()
  const queryClient = useQueryClient()
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
      queryClient.clear()
      await login(String(data.get('email')), String(data.get('password')))
      // Rol yeni token'dan okunur: satıcıysa panele, değilse başvuru sayfasına.
      if (hasSellerRole(tokenStore.get()?.user ?? null)) {
        navigate((location.state as { from?: string } | null)?.from ?? '/', { replace: true })
      } else {
        navigate('/basvuru', { replace: true })
      }
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Giriş yapılamadı.')
      setPending(false)
    }
  }

  return (
    <AuthCard title="Giriş yap" footer={<>Hesabınız yok mu? <Link to="/kayit" className="font-semibold text-brand-700 underline">Üye olun</Link></>}>
      <form onSubmit={onSubmit} className="flex flex-col gap-4" noValidate aria-busy={pending}>
        {error && <Alert>{error}</Alert>}
        <Field label="E-posta" name="email" type="email" autoComplete="email" />
        <Field label="Şifre" name="password" type="password" autoComplete="current-password" />
        <button disabled={pending} className={`${primaryButton} py-3`}>{pending ? 'Giriş yapılıyor…' : 'Giriş yap'}</button>
      </form>
    </AuthCard>
  )
}
