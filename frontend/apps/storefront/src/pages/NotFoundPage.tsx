import { Link } from 'react-router'

export function NotFoundPage() {
  return (
    <div className="rounded-xl border border-line bg-white px-6 py-16 text-center">
      <h1 className="text-xl font-bold">Sayfa bulunamadı</h1>
      <Link to="/" className="mt-4 inline-block text-brand-700 underline">Ana sayfaya dön</Link>
    </div>
  )
}
