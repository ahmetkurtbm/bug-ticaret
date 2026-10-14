// Türkçe fiyat biçimi: binlik ayracı nokta (isteğe bağlı), ondalık ayracı virgül, en çok 2 ondalık.
const GROUPED = /^\d{1,3}(\.\d{3})+(,\d{1,2})?$/
const PLAIN = /^\d+(,\d{1,2})?$/

/** "1.249,90" -> 1249.9. Geçersiz ya da 2 ondalığı aşan girdide null döner ("249.90" da geçersizdir: nokta binlik ayracıdır). */
export function parsePrice(input: string): number | null {
  const text = input.trim()
  if (!GROUPED.test(text) && !PLAIN.test(text)) return null
  const value = Number(text.replace(/\./g, '').replace(',', '.'))
  return Number.isFinite(value) ? value : null
}

const inputFormat = new Intl.NumberFormat('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })

/** Sayıyı input'ta göstermek için: 1249.9 -> "1.249,90". */
export const formatPriceInput = (value: number | null) => (value === null ? '' : inputFormat.format(value))
