import type { InputHTMLAttributes, ReactNode } from 'react'

export function AuthCard({ title, children, footer }: { title: string; children: ReactNode; footer: ReactNode }) {
  return (
    <div className="mx-auto w-full max-w-md rounded-2xl border border-line bg-white p-6 sm:p-8">
      <h1 className="mb-6 text-2xl font-bold">{title}</h1>
      {children}
      <p className="mt-6 text-sm text-muted">{footer}</p>
    </div>
  )
}

export function Field({ label, name, error, hint, ...props }: InputHTMLAttributes<HTMLInputElement> & { label: string; name: string; error?: string; hint?: string }) {
  const id = `f-${name}`
  return (
    <div className="flex flex-col gap-1.5">
      <label htmlFor={id} className="text-sm font-medium">{label}</label>
      <input
        id={id}
        name={name}
        aria-invalid={error ? true : undefined}
        aria-describedby={error || hint ? `${id}-msg` : undefined}
        className="rounded-lg border border-line px-3 py-2.5 aria-invalid:border-red-500"
        {...props}
      />
      {(error || hint) && <p id={`${id}-msg`} className={`text-xs ${error ? 'text-red-700' : 'text-muted'}`}>{error ?? hint}</p>}
    </div>
  )
}
