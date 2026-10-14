import type { InputHTMLAttributes, ReactNode, SelectHTMLAttributes } from 'react'

const controlClass = 'rounded-lg border border-line bg-white px-3 py-2 text-sm aria-invalid:border-red-500'

export function Field({ label, name, error, hint, ...props }: InputHTMLAttributes<HTMLInputElement> & { label: string; name: string; error?: string; hint?: string }) {
  const id = `f-${name}`
  return (
    <div className="flex min-w-0 flex-col gap-1.5">
      <label htmlFor={id} className="text-sm font-medium">{label}</label>
      <input
        id={id}
        name={name}
        aria-invalid={error ? true : undefined}
        aria-describedby={error || hint ? `${id}-msg` : undefined}
        className={controlClass}
        {...props}
      />
      {(error || hint) && <p id={`${id}-msg`} className={`text-xs ${error ? 'text-red-700' : 'text-muted'}`}>{error ?? hint}</p>}
    </div>
  )
}

export function SelectField({ label, name, error, children, ...props }: SelectHTMLAttributes<HTMLSelectElement> & { label: string; name: string; error?: string; children: ReactNode }) {
  const id = `f-${name}`
  return (
    <div className="flex min-w-0 flex-col gap-1.5">
      <label htmlFor={id} className="text-sm font-medium">{label}</label>
      <select id={id} name={name} aria-invalid={error ? true : undefined} aria-describedby={error ? `${id}-msg` : undefined} className={controlClass} {...props}>
        {children}
      </select>
      {error && <p id={`${id}-msg`} className="text-xs text-red-700">{error}</p>}
    </div>
  )
}
