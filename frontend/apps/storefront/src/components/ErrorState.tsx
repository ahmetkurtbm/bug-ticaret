import { ApiError } from '../api/client'

export function ErrorState({ error, onRetry }: { error: unknown; onRetry?: () => void }) {
  const message = error instanceof ApiError ? error.message : 'Bağlantı kurulamadı. İnternetinizi kontrol edip tekrar deneyin.'
  return (
    <div role="alert" className="rounded-xl border border-line bg-white px-6 py-10 text-center">
      <p className="font-semibold">{message}</p>
      {onRetry && (
        <button onClick={onRetry} className="mt-4 rounded-lg border border-line px-4 py-2 text-sm hover:bg-surface">
          Tekrar dene
        </button>
      )}
    </div>
  )
}
