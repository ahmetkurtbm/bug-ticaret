import type { Tone } from '../lib/statuses'

const toneClass: Record<Tone, string> = {
  neutral: 'bg-line text-muted',
  success: 'bg-emerald-100 text-emerald-900',
  warning: 'bg-amber-100 text-amber-900',
  danger: 'bg-red-100 text-red-800',
  info: 'bg-sky-100 text-sky-900',
}

export function StatusBadge({ label, tone }: { label: string; tone: Tone }) {
  return <span className={`inline-block whitespace-nowrap rounded-full px-2.5 py-0.5 text-xs font-semibold ${toneClass[tone]}`}>{label}</span>
}

/** Enum değeri → etiket/renk eşlemesi olan rozet. */
export function MetaBadge<K extends string | number>({ value, meta }: { value: K; meta: Record<K, { label: string; tone: Tone }> }) {
  const entry = meta[value]
  return <StatusBadge label={entry?.label ?? String(value)} tone={entry?.tone ?? 'neutral'} />
}
