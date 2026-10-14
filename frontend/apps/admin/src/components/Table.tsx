import type { ReactNode, ThHTMLAttributes } from 'react'

/** Tablo kendi kapsayıcısında yatay kayar; sayfa kaymaz. */
export function TableShell({ caption, children }: { caption: string; children: ReactNode }) {
  return (
    <div className="overflow-x-auto rounded-xl border border-line bg-white">
      <table className="w-full min-w-max text-left text-sm">
        <caption className="sr-only">{caption}</caption>
        {children}
      </table>
    </div>
  )
}

export function Th({ children, className = '', ...props }: ThHTMLAttributes<HTMLTableCellElement>) {
  return (
    <th scope="col" className={`whitespace-nowrap border-b border-line bg-surface px-3 py-2.5 text-xs font-semibold uppercase tracking-wide text-muted ${className}`} {...props}>
      {children}
    </th>
  )
}

export function RowTh({ children, className = '' }: { children: ReactNode; className?: string }) {
  return <th scope="row" className={`px-3 py-3 text-left font-semibold ${className}`}>{children}</th>
}

export const tdClass = 'px-3 py-3 align-top'
export const rowClass = 'border-b border-line last:border-0'
