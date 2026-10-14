import { ApiError } from '../api/client'

export function ErrorState({ error, onRetry }: { error: unknown; onRetry?: () => void }) {
  const message = error instanceof ApiError ? error.message : 'Bağlantı kurulamadı. İnternetinizi kontrol edip tekrar deneyin.'
  return (
    <div role="alert" className="rounded-xl border border-line bg-white px-6 py-10 text-center">
      <p className="font-semibold">{message}</p>
      {onRetry && (
        <button type="button" onClick={onRetry} className="mt-4 rounded-lg border border-line px-4 py-2 text-sm hover:bg-surface">
          Tekrar dene
        </button>
      )}
    </div>
  )
}

export function LoadingBlock({ className = 'h-72' }: { className?: string }) {
  return <div className={`animate-pulse rounded-xl bg-line/60 ${className}`} aria-busy="true" role="status" aria-label="Yükleniyor" />
}

/** Mutasyon hatası için satır içi uyarı. */
export function InlineError({ error }: { error: unknown }) {
  if (!error) return null
  const message = error instanceof ApiError ? error.message : 'İşlem tamamlanamadı. Bağlantınızı kontrol edip tekrar deneyin.'
  return <p role="alert" className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{message}</p>
}
