export function Pagination({ page, totalPages, onChange }: { page: number; totalPages: number; onChange: (page: number) => void }) {
  if (totalPages <= 1) return null
  const pages = Array.from(new Set([1, page - 1, page, page + 1, totalPages])).filter((p) => p >= 1 && p <= totalPages)

  return (
    <nav className="mt-8 flex items-center justify-center gap-1" aria-label="Sayfalar">
      <PageButton disabled={page === 1} onClick={() => onChange(page - 1)} label="Önceki" />
      {pages.map((p, i) => (
        <span key={p} className="flex items-center gap-1">
          {i > 0 && p - pages[i - 1] > 1 && <span className="px-1 text-muted">…</span>}
          <PageButton active={p === page} onClick={() => onChange(p)} label={String(p)} />
        </span>
      ))}
      <PageButton disabled={page === totalPages} onClick={() => onChange(page + 1)} label="Sonraki" />
    </nav>
  )
}

function PageButton({ label, onClick, active, disabled }: { label: string; onClick: () => void; active?: boolean; disabled?: boolean }) {
  return (
    <button
      onClick={onClick}
      disabled={disabled}
      aria-current={active ? 'page' : undefined}
      className={`min-w-9 rounded-md border px-3 py-1.5 text-sm ${active ? 'border-brand-700 bg-brand-700 text-white' : 'border-line bg-white hover:bg-surface'} disabled:opacity-40`}
    >
      {label}
    </button>
  )
}
