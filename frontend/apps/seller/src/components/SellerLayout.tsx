import { useQuery } from '@tanstack/react-query'
import { Navigate, NavLink, Outlet, useLocation } from 'react-router'
import { ApiError } from '../api/client'
import { sellerApi } from '../api/seller'
import { useAuth } from '../auth/AuthContext'
import { useSignOut } from '../auth/useSignOut'
import { hasSellerRole } from '../lib/roles'
import { ErrorState } from './ErrorState'
import { secondaryButton } from './Field'

const navItems = [
  { to: '/', label: 'Özet', end: true },
  { to: '/urunler', label: 'Ürünler', end: false },
  { to: '/siparisler', label: 'Siparişler', end: false },
]

export function SellerLayout() {
  const { user } = useAuth()
  const location = useLocation()
  const signOut = useSignOut()
  const isSeller = hasSellerRole(user)
  const me = useQuery({ queryKey: ['seller', 'me'], queryFn: ({ signal }) => sellerApi.me(signal), enabled: isSeller })

  if (!user) return <Navigate to="/giris" replace state={{ from: location.pathname + location.search }} />
  if (!isSeller) return <Navigate to="/basvuru" replace />

  // Rol var ama mağaza aktif değil (askıda vb.): sebep sunucudan gelir.
  if (me.error instanceof ApiError && me.error.status === 403) {
    return (
      <div className="mx-auto flex min-h-dvh max-w-xl flex-col justify-center gap-4 px-4">
        <ErrorState error={me.error} />
        <button onClick={() => void signOut()} className={secondaryButton}>Çıkış yap</button>
      </div>
    )
  }

  return (
    <div className="flex min-h-dvh flex-col">
      <header className="border-b border-line bg-white">
        <div className="mx-auto flex w-full max-w-6xl flex-wrap items-center justify-between gap-x-6 gap-y-2 px-4 py-3 sm:px-6">
          <div className="min-w-0">
            <p className="text-xs font-semibold uppercase tracking-wide text-brand-700">Tezgah Satıcı Paneli</p>
            <p className="truncate text-lg font-bold">{me.data?.storeName ?? ' '}</p>
          </div>
          <div className="flex items-center gap-3 text-sm">
            <span className="hidden text-muted sm:inline">{user.email}</span>
            <button onClick={() => void signOut()} className="font-semibold text-brand-700 hover:underline">Çıkış yap</button>
          </div>
        </div>
        <nav aria-label="Panel menüsü" className="mx-auto flex w-full max-w-6xl gap-1 px-4 sm:px-6">
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.end}
              className={({ isActive }) =>
                `border-b-2 px-3 py-2.5 text-sm font-medium ${isActive ? 'border-brand-700 text-brand-700' : 'border-transparent text-muted hover:text-ink'}`
              }
            >
              {item.label}
            </NavLink>
          ))}
        </nav>
      </header>
      <main className="mx-auto w-full max-w-6xl flex-1 px-4 py-6 sm:px-6">
        <Outlet />
      </main>
    </div>
  )
}
