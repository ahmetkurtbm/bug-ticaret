import type { AuthResponse } from '../api/types'

const KEY = 'tezgah.admin.session'
type Listener = (session: AuthResponse | null) => void
const listeners = new Set<Listener>()

function read(): AuthResponse | null {
  try {
    const raw = localStorage.getItem(KEY)
    return raw ? (JSON.parse(raw) as AuthResponse) : null
  } catch {
    return null
  }
}

let current = read()

// Başka bir sekme oturumu yenileyince ya da çıkış yapınca bu sekme de aynı oturumu görür.
if (typeof window !== 'undefined') {
  window.addEventListener('storage', (event) => {
    if (event.key !== KEY && event.key !== null) return
    current = read()
    listeners.forEach((l) => l(current))
  })
}

/** Oturum bilgisi. React dışındaki API istemcisi de buradan okur. */
export const tokenStore = {
  get: () => current,
  /** Depodaki son oturumu yeniden okur (başka bir sekmenin yazdığı dahil). */
  reload() {
    current = read()
    return current
  },
  set(session: AuthResponse) {
    current = session
    localStorage.setItem(KEY, JSON.stringify(session))
    listeners.forEach((l) => l(session))
  },
  clear() {
    current = null
    localStorage.removeItem(KEY)
    listeners.forEach((l) => l(null))
  },
  subscribe(listener: Listener) {
    listeners.add(listener)
    return () => {
      listeners.delete(listener)
    }
  },
}
