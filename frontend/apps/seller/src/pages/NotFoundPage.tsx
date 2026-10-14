import { Link } from 'react-router'
import { PlainShell } from './authUi'

export function NotFoundPage() {
  return (
    <PlainShell>
      <div className="rounded-xl border border-line bg-white px-6 py-16 text-center">
        <h1 className="text-xl font-bold">Sayfa bulunamadı</h1>
        <Link to="/" className="mt-4 inline-block text-brand-700 underline">Panele dön</Link>
      </div>
    </PlainShell>
  )
}
