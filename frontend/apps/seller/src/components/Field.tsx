import type { InputHTMLAttributes, ReactNode, SelectHTMLAttributes, TextareaHTMLAttributes } from 'react'

export const controlClass = 'w-full min-w-0 rounded-lg border border-line bg-white px-3 py-2.5 aria-invalid:border-red-500'

interface Shared {
  label: string
  name: string
  error?: string
  hint?: string
}

function Shell({ id, label, error, hint, children }: { id: string; label: string; error?: string; hint?: string; children: ReactNode }) {
  return (
    <div className="flex min-w-0 flex-col gap-1.5">
      <label htmlFor={id} className="text-sm font-medium">{label}</label>
      {children}
      {(error || hint) && <p id={`${id}-msg`} className={`text-xs ${error ? 'text-red-700' : 'text-muted'}`}>{error ?? hint}</p>}
    </div>
  )
}

const aria = (id: string, error?: string, hint?: string) => ({
  'aria-invalid': error ? true : undefined,
  'aria-describedby': error || hint ? `${id}-msg` : undefined,
})

export function Field({ label, name, error, hint, id: idProp, ...props }: InputHTMLAttributes<HTMLInputElement> & Shared) {
  const id = idProp ?? `f-${name}`
  return (
    <Shell id={id} label={label} error={error} hint={hint}>
      <input id={id} name={name} className={controlClass} {...aria(id, error, hint)} {...props} />
    </Shell>
  )
}

export function TextAreaField({ label, name, error, hint, id: idProp, ...props }: TextareaHTMLAttributes<HTMLTextAreaElement> & Shared) {
  const id = idProp ?? `f-${name}`
  return (
    <Shell id={id} label={label} error={error} hint={hint}>
      <textarea id={id} name={name} className={controlClass} {...aria(id, error, hint)} {...props} />
    </Shell>
  )
}

export function SelectField({ label, name, error, hint, id: idProp, children, ...props }: SelectHTMLAttributes<HTMLSelectElement> & Shared) {
  const id = idProp ?? `f-${name}`
  return (
    <Shell id={id} label={label} error={error} hint={hint}>
      <select id={id} name={name} className={controlClass} {...aria(id, error, hint)} {...props}>
        {children}
      </select>
    </Shell>
  )
}

/** Hata sözlüğünde büyük/küçük harfe bakmadan anahtar arar ("variants[0].Price" ~ "variants[0].price"). */
export function errorLookup(errors: Record<string, string>) {
  const lower = new Map(Object.entries(errors).map(([k, v]) => [k.toLowerCase(), v]))
  return (key: string) => lower.get(key.toLowerCase())
}

export function Alert({ children, tone = 'error' }: { children: ReactNode; tone?: 'error' | 'success' }) {
  return (
    <p role={tone === 'error' ? 'alert' : 'status'} className={`rounded-lg px-3 py-2 text-sm ${tone === 'error' ? 'bg-red-50 text-red-700' : 'bg-emerald-50 text-emerald-800'}`}>
      {children}
    </p>
  )
}

export const primaryButton = 'rounded-lg bg-brand-700 px-5 py-2.5 text-sm font-semibold text-white hover:bg-brand-800 disabled:opacity-60'
export const secondaryButton = 'rounded-lg border border-line bg-white px-4 py-2.5 text-sm font-medium hover:bg-surface disabled:opacity-60'
