import { ApiError } from '../api/client'

export function ErrorState({ error, onRetry }: { error: unknown; onRetry?: () => void }) {
  const forbidden = error instanceof ApiError && error.status === 403
  const message = error instanceof ApiError ? error.message : 'Bağlantı kurulamadı. İnternetinizi kontrol edip tekrar deneyin.'
  return (
    <div role="alert" className="rounded-xl border border-line bg-white px-6 py-10 text-center">
      {forbidden && <h1 className="mb-2 text-lg font-bold">Bu sayfaya erişiminiz yok</h1>}
      <p className={forbidden ? 'text-muted' : 'font-semibold'}>{message}</p>
      {onRetry && (
        <button onClick={onRetry} className="mt-4 rounded-lg border border-line px-4 py-2 text-sm hover:bg-surface">
          Tekrar dene
        </button>
      )}
    </div>
  )
}

export function LoadingBlock({ className = 'h-72' }: { className?: string }) {
  return <div className={`${className} animate-pulse rounded-xl bg-line/60`} aria-busy="true" aria-label="Yükleniyor" />
}
