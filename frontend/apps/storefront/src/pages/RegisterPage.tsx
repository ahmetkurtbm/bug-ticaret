import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router'
import { ApiError } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { AuthCard, Field } from './authUi'

export function RegisterPage() {
  const { register } = useAuth()
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
      await register({
        firstName: String(data.get('firstName')),
        lastName: String(data.get('lastName')),
        email: String(data.get('email')),
        password: String(data.get('password')),
      })
      navigate('/', { replace: true })
    } catch (err) {
      if (err instanceof ApiError) {
        setFieldErrors(err.fieldErrors)
        if (Object.keys(err.fieldErrors).length === 0) setError(err.message)
      } else setError('Kayıt yapılamadı.')
    } finally {
      setPending(false)
    }
  }

  return (
    <AuthCard title="Üye ol" footer={<>Zaten üye misiniz? <Link to="/giris" className="font-semibold text-brand-700 underline">Giriş yapın</Link></>}>
      <form onSubmit={onSubmit} className="flex flex-col gap-4" noValidate>
        {error && <p role="alert" className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <div className="grid grid-cols-2 gap-3">
          <Field label="Ad" name="firstName" autoComplete="given-name" error={fieldErrors.firstName} />
          <Field label="Soyad" name="lastName" autoComplete="family-name" error={fieldErrors.lastName} />
        </div>
        <Field label="E-posta" name="email" type="email" autoComplete="email" error={fieldErrors.email} />
        <Field label="Şifre" name="password" type="password" autoComplete="new-password" hint="En az 8 karakter" error={fieldErrors.password} />
        <button disabled={pending} className="rounded-lg bg-brand-700 py-3 font-semibold text-white hover:bg-brand-800 disabled:opacity-60">
          {pending ? 'Hesap açılıyor…' : 'Üye ol'}
        </button>
      </form>
    </AuthCard>
  )
}
