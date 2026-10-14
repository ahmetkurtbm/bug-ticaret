const currency = new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY' })

export const formatPrice = (value: number) => currency.format(value)

export function discountPercent(price: number, listPrice: number | null) {
  if (!listPrice || listPrice <= price) return null
  return Math.round((1 - price / listPrice) * 100)
}
