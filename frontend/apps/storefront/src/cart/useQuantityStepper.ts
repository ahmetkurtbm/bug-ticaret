import { useCallback, useEffect, useRef, useState } from 'react'

export const QUANTITY_DEBOUNCE_MS = 400

/**
 * Sepet satırındaki adet düğmeleri. Her tıklama ekrandaki adedi hemen değiştirir; sunucuya, kullanıcı
 * tıklamayı bıraktıktan QUANTITY_DEBOUNCE_MS sonra tek istek gider (art arda 5 tıklama = 1 istek).
 *
 * `change` bir kez oluşturulur; düğmeler her tıklamada yeniden çizilmesin.
 */
export function useQuantityStepper(serverQuantity: number, max: number, commit: (quantity: number) => Promise<unknown> | void) {
  // Sunucuya henüz gitmemiş (ya da yanıtı beklenen) adet; yoksa sunucudaki adet gösterilir.
  const [pending, setPending] = useState<number | null>(null)
  const latest = useRef<number | null>(null)
  const serverRef = useRef(serverQuantity)
  const commitRef = useRef(commit)
  const timer = useRef<ReturnType<typeof setTimeout> | undefined>(undefined)

  useEffect(() => {
    serverRef.current = serverQuantity
    commitRef.current = commit
  })
  useEffect(() => () => clearTimeout(timer.current), [])

  const change = useCallback(
    (delta: number) => {
      const base = pending ?? serverQuantity
      const next = Math.min(max, Math.max(1, base + delta))
      if (next === base) return
      latest.current = next
      setPending(next)
      clearTimeout(timer.current)
      timer.current = setTimeout(async () => {
        const value = latest.current
        if (value === null) return
        try {
          await commitRef.current(value)
        } catch {
          // Hata mesajını çağıran gösterir (mutation.error); burada yalnızca bekleyen adet temizlenir.
        } finally {
          if (latest.current === value) {
            latest.current = null
            setPending(null)
          }
        }
      }, QUANTITY_DEBOUNCE_MS)
    },
    [max], // eslint-disable-line react-hooks/exhaustive-deps
  )

  return { quantity: pending ?? serverQuantity, change }
}
