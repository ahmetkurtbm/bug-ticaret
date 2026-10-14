const TIME_ZONE = 'Europe/Istanbul'

const dateTimeFormat = new Intl.DateTimeFormat('tr-TR', { dateStyle: 'medium', timeStyle: 'short', timeZone: TIME_ZONE })
const dayFormat = new Intl.DateTimeFormat('tr-TR', { dateStyle: 'medium', timeZone: TIME_ZONE })
// "YYYY-MM-DD" günleri saat dilimi kaymasın diye UTC gece yarısı olarak biçimlenir.
const plainDayFormat = new Intl.DateTimeFormat('tr-TR', { dateStyle: 'medium', timeZone: 'UTC' })
const shortDayFormat = new Intl.DateTimeFormat('tr-TR', { day: 'numeric', month: 'short', timeZone: 'UTC' })
const todayParts = new Intl.DateTimeFormat('en-CA', { year: 'numeric', month: '2-digit', day: '2-digit', timeZone: TIME_ZONE })

const DAY_PATTERN = /^\d{4}-\d{2}-\d{2}$/
const DAY_MS = 86_400_000

export const formatDateTime = (iso: string) => dateTimeFormat.format(new Date(iso))
export const formatDay = (iso: string) => dayFormat.format(new Date(iso))
export const formatPlainDay = (day: string) => plainDayFormat.format(new Date(`${day}T00:00:00Z`))
export const formatShortDay = (day: string) => shortDayFormat.format(new Date(`${day}T00:00:00Z`))

/** Kuponun bitişi ertesi gün 00:00 (dışlayıcı) saklanır; kullanıcıya bitiş gününün kendisi (dahil) gösterilir. */
export const formatInclusiveEndDay = (endsAtIso: string) => dayFormat.format(new Date(Date.parse(endsAtIso) - 1))

export const isDayString = (value: string | null): value is string => value !== null && DAY_PATTERN.test(value) && !Number.isNaN(Date.parse(value))

export const todayInIstanbul = () => todayParts.format(new Date())

export function addDays(day: string, amount: number) {
  return new Date(Date.parse(`${day}T00:00:00Z`) + amount * DAY_MS).toISOString().slice(0, 10)
}

export const startOfMonth = (day: string) => `${day.slice(0, 8)}01`

/** İki gün arasındaki fark (to - from). */
export const daysBetween = (from: string, to: string) => Math.round((Date.parse(`${to}T00:00:00Z`) - Date.parse(`${from}T00:00:00Z`)) / DAY_MS)
