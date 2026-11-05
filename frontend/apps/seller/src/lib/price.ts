// Türkçe fiyat biçimi: binlik ayracı nokta (isteğe bağlı), ondalık ayracı virgül, en çok 2 ondalık.

/** Fiyat girdisini sayıya çevirir. Hem "249,90" hem "249.90" kabul edilir; satıcılar ikisini de yazıyor. */
export function parsePrice(input: string): number | null {
  const value = Number.parseFloat(input.trim().replace(',', '.'))
  return Number.isFinite(value) && value > 0 ? Math.round(value * 100) / 100 : null
}

const inputFormat = new Intl.NumberFormat('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })

/** Sayıyı input'ta göstermek için: 1249.9 -> "1.249,90". */
export const formatPriceInput = (value: number | null) => (value === null ? '' : inputFormat.format(value))
