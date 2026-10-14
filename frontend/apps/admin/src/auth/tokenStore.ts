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

/** Oturum bilgisi. React dışındaki API istemcisi de buradan okur. */
export const tokenStore = {
  get: () => current,
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
