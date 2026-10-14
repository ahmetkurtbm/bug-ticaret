import { useState, type FormEvent } from 'react'
import { Link, NavLink, useNavigate, useSearchParams } from 'react-router'
import { useAuth } from '../auth/AuthContext'
import { useCart } from '../cart/useCart'
import { CategoryMenu } from './CategoryMenu'

export function Header() {
  const navigate = useNavigate()
  const [params] = useSearchParams()
  const [query, setQuery] = useState(params.get('q') ?? '')
  const { user, logout } = useAuth()
  const { data: cart } = useCart()

  function onSearch(e: FormEvent) {
    e.preventDefault()
    const q = query.trim()
    if (q) navigate(`/ara?q=${encodeURIComponent(q)}`)
  }

  return (
    <header className="sticky top-0 z-20 border-b border-line bg-white">
      <div className="mx-auto flex max-w-7xl flex-wrap items-center gap-x-4 gap-y-2 px-4 py-3 sm:flex-nowrap sm:px-6">
        <Link to="/" className="text-2xl font-extrabold tracking-tight text-brand-700">
          tezgah
        </Link>

        <form onSubmit={onSearch} className="order-last flex w-full sm:order-none sm:w-auto sm:flex-1" role="search">
          <label htmlFor="search" className="sr-only">
            Ürün ara
          </label>
          <input
            id="search"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="Ürün, kategori veya marka ara"
            className="w-full rounded-l-lg border border-r-0 border-line bg-surface px-4 py-2.5 text-sm outline-none focus:border-brand-600 focus:bg-white"
          />
          <button type="submit" className="rounded-r-lg bg-brand-700 px-4 text-sm font-semibold text-white hover:bg-brand-800">
            Ara
          </button>
        </form>

        <nav className="ml-auto flex items-center gap-1 text-sm font-medium sm:ml-0" aria-label="Hesap">
          {user ? (
            <>
              <span className="hidden px-2 text-muted md:inline">Merhaba, {user.firstName}</span>
              <NavLink to="/hesabim/siparisler" className="rounded-md px-2 py-2 hover:bg-surface">
                Siparişlerim
              </NavLink>
              <button onClick={() => void logout()} className="rounded-md px-2 py-2 hover:bg-surface">
                Çıkış
              </button>
            </>
          ) : (
            <NavLink to="/giris" className="rounded-md px-2 py-2 hover:bg-surface">
              Giriş yap
            </NavLink>
          )}
          <NavLink to="/sepet" className="relative flex items-center gap-1.5 rounded-md px-2 py-2 hover:bg-surface">
            Sepetim
            {cart && cart.itemCount > 0 && (
              <span className="min-w-5 rounded-full bg-accent-500 px-1.5 text-center text-xs font-bold text-white" aria-label={`${cart.itemCount} ürün`}>
                {cart.itemCount}
              </span>
            )}
          </NavLink>
        </nav>
      </div>
      <CategoryMenu />
    </header>
  )
}
