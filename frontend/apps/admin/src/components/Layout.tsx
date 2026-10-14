import { NavLink, Outlet } from 'react-router'
import { useAuth } from '../auth/AuthContext'

const links = [
  { to: '/', label: 'Gösterge', end: true },
  { to: '/siparisler', label: 'Siparişler', end: false },
  { to: '/saticilar', label: 'Satıcılar', end: false },
  { to: '/urun-onayi', label: 'Ürün onayı', end: false },
  { to: '/kampanyalar', label: 'Kampanyalar', end: false },
]

export function Layout() {
  const { user, logout } = useAuth()
  return (
    <div className="flex min-h-screen flex-col md:flex-row">
      <aside className="border-b border-line bg-white md:w-56 md:shrink-0 md:border-r md:border-b-0">
        <div className="flex flex-wrap items-center justify-between gap-2 px-4 py-3 md:block md:py-5">
          <p className="text-lg font-bold text-brand-700">Tezgah <span className="text-sm font-medium text-muted">Yönetim</span></p>
          <div className="md:mt-1 md:text-xs">
            <span className="mr-2 text-xs text-muted md:mr-0 md:block md:truncate">{user?.email}</span>
            <button type="button" onClick={() => void logout()} className="text-xs font-semibold text-brand-700 underline md:mt-1">Çıkış yap</button>
          </div>
        </div>
        <nav aria-label="Ana menü" className="px-2 pb-2 md:px-3 md:pb-4">
          <ul className="flex flex-wrap gap-1 md:flex-col">
            {links.map((l) => (
              <li key={l.to}>
                <NavLink
                  to={l.to}
                  end={l.end}
                  className={({ isActive }) => `block rounded-lg px-3 py-2 text-sm font-medium ${isActive ? 'bg-brand-50 text-brand-800' : 'text-ink hover:bg-surface'}`}
                >
                  {l.label}
                </NavLink>
              </li>
            ))}
          </ul>
        </nav>
      </aside>
      <main className="min-w-0 flex-1 px-4 py-6 md:px-8">
        <Outlet />
      </main>
    </div>
  )
}
