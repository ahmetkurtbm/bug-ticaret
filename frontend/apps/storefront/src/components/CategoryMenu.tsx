import { useQuery } from '@tanstack/react-query'
import { NavLink } from 'react-router'
import { catalogApi } from '../api/catalog'

export function CategoryMenu() {
  const { data: categories } = useQuery({
    queryKey: ['categories'],
    queryFn: ({ signal }) => catalogApi.categories(signal),
    staleTime: 10 * 60_000,
  })

  return (
    <nav className="border-t border-line" aria-label="Kategoriler">
      <ul className="mx-auto flex max-w-7xl gap-1 overflow-x-auto px-4 text-sm [scrollbar-width:none] sm:px-6">
        {categories?.map((c) => (
          <li key={c.id}>
            <NavLink
              to={`/kategori/${c.slug}`}
              className={({ isActive }) =>
                `block whitespace-nowrap border-b-2 px-3 py-2.5 ${isActive ? 'border-brand-600 font-semibold text-brand-700' : 'border-transparent text-muted hover:text-ink'}`
              }
            >
              {c.name}
            </NavLink>
          </li>
        ))}
      </ul>
    </nav>
  )
}
