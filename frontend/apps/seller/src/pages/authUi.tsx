import type { ReactNode } from 'react'

/** Giriş/kayıt/başvuru/404 gibi panel dışı sayfaların ortak çerçevesi. */
export function PlainShell({ children, wide }: { children: ReactNode; wide?: boolean }) {
  return (
    <div className="flex min-h-dvh flex-col items-center px-4 py-8 sm:py-12">
      <p className="mb-6 text-sm font-semibold uppercase tracking-wide text-brand-700">Tezgah Satıcı Paneli</p>
      <div className={`w-full ${wide ? 'max-w-xl' : 'max-w-md'}`}>{children}</div>
    </div>
  )
}

export function AuthCard({ title, children, footer }: { title: string; children: ReactNode; footer?: ReactNode }) {
  return (
    <PlainShell>
      <div className="rounded-2xl border border-line bg-white p-6 sm:p-8">
        <h1 className="mb-6 text-2xl font-bold">{title}</h1>
        {children}
        {footer && <p className="mt-6 text-sm text-muted">{footer}</p>}
      </div>
    </PlainShell>
  )
}
