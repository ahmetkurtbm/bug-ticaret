import { useAuth } from '../auth/AuthContext'

export function AccessDenied() {
  const { logout } = useAuth()
  return (
    <main className="mx-auto flex min-h-screen max-w-md items-center px-4">
      <div role="alert" className="w-full rounded-2xl border border-line bg-white p-6 text-center sm:p-8">
        <h1 className="text-xl font-bold">Bu panele erişim yetkiniz yok</h1>
        <p className="mt-2 text-sm text-muted">Yönetim paneli yalnızca yönetici hesaplarına açıktır. Farklı bir hesapla giriş yapabilirsiniz.</p>
        <button type="button" onClick={() => void logout()} className="mt-6 rounded-lg bg-brand-700 px-5 py-2.5 font-semibold text-white hover:bg-brand-800">
          Çıkış yap
        </button>
      </div>
    </main>
  )
}
