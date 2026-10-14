export function Rating({ value, count }: { value: number; count: number }) {
  return (
    <span className="flex items-center gap-1 text-xs text-muted" aria-label={`5 üzerinden ${value.toFixed(1)}, ${count} değerlendirme`}>
      <span className="font-semibold text-amber-600">★ {value.toFixed(1)}</span>
      <span>({count.toLocaleString('tr-TR')})</span>
    </span>
  )
}
