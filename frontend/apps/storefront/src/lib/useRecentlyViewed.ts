import { useEffect } from 'react'

const KEY = 'tezgah.recentlyViewed'
const MAX = 12

/** Son bakılan ürünleri (en yeni başta) tarayıcıda saklar. Ana sayfadaki "Son baktıkların" şeridi okuyacak. */
export function useRecentlyViewed(productId: number) {
  useEffect(() => {
    try {
      const list = JSON.parse(localStorage.getItem(KEY) ?? '[]') as number[]
      localStorage.setItem(KEY, JSON.stringify([productId, ...list.filter((id) => id !== productId)].slice(0, MAX)))
    } catch {
      // Gizli sekme ya da dolu depolama: kayıt tutulamazsa sorun değil.
    }
  }, [productId])
}
