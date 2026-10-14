import { useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router'
import { ApiError } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { Alert, Field, primaryButton } from '../components/Field'
import { AuthCard } from './authUi'

export function RegisterPage() {
  const { register } = useAuth()
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({})
  const [pending, setPending] = useState(false)

  async function onSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    const data = new FormData(e.currentTarget)
    setPending(true)
    setError(null)
    setFieldErrors({})
    try {
      queryClient.clear()
      await register({
        firstName: String(data.get('firstName')),
        lastName: String(data.get('lastName')),
        email: String(data.get('email')),
        password: String(data.get('password')),
      })
      // Yeni hesap satıcı değildir; önce mağaza başvurusu yapılır.
      navigate('/basvuru', { replace: true })
    } catch (err) {
      if (err instanceof ApiError) {
        setFieldErrors(err.fieldErrors)
        if (Object.keys(err.fieldErrors).length === 0) setError(err.message)
      } else setError('Kayıt yapılamadı.')
      setPending(false)
    }
  }

  return (
    <AuthCard title="Üye ol" footer={<>Zaten üye misiniz? <Link to="/giris" className="font-semibold text-brand-700 underline">Giriş yapın</Link></>}>
      <form onSubmit={onSubmit} className="flex flex-col gap-4" noValidate aria-busy={pending}>
        {error && <Alert>{error}</Alert>}
        <div className="grid grid-cols-2 gap-3">
          <Field label="Ad" name="firstName" autoComplete="given-name" error={fieldErrors.firstName} />
          <Field label="Soyad" name="lastName" autoComplete="family-name" error={fieldErrors.lastName} />
        </div>
        <Field label="E-posta" name="email" type="email" autoComplete="email" error={fieldErrors.email} />
        <Field label="Şifre" name="password" type="password" autoComplete="new-password" hint="En az 8 karakter" error={fieldErrors.password} />
        <button disabled={pending} className={`${primaryButton} py-3`}>{pending ? 'Hesap açılıyor…' : 'Üye ol'}</button>
      </form>
    </AuthCard>
  )
}
