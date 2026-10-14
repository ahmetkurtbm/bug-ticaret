// Bölüm FE-14 kontrolü: Türkçe biçimli fiyat girdisi doğru sayıya çevrilmeli.
import { expect, test } from 'vitest'
import { parsePrice } from '../lib/price'

test.each([
  ['1.249,90', 1249.9],
  ['249,90', 249.9],
  ['1249,9', 1249.9],
  ['12.500', 12500],
  ['0,99', 0.99],
  ['1.000.000', 1000000],
])('"%s" -> %d', (input, expected) => {
  expect(parsePrice(input)).toBe(expected)
})

test.each(['', 'abc', '12,345', '1,2,3', '1.24,90'])('"%s" geçersiz', (input) => {
  expect(parsePrice(input)).toBeNull()
})
