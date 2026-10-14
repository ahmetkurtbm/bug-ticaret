// Bölüm FE-03 kontrolü: adet düğmesine art arda basınca sunucuya son adet gitmeli.
import { act, renderHook } from '@testing-library/react'
import { afterEach, beforeEach, expect, test, vi } from 'vitest'
import { QUANTITY_DEBOUNCE_MS, useQuantityStepper } from '../cart/useQuantityStepper'

beforeEach(() => vi.useFakeTimers())
afterEach(() => vi.useRealTimers())

test('art arda üç kez + basılınca adet 4 olarak tek istekte gider', () => {
  const commit = vi.fn()
  const { result } = renderHook(() => useQuantityStepper(1, 20, commit))

  act(() => result.current.change(1))
  act(() => result.current.change(1))
  act(() => result.current.change(1))
  expect(result.current.quantity).toBe(4)

  act(() => vi.advanceTimersByTime(QUANTITY_DEBOUNCE_MS))
  expect(commit).toHaveBeenCalledTimes(1)
  expect(commit).toHaveBeenLastCalledWith(4)
})

test('alt sınır 1, üst sınır stok', () => {
  const commit = vi.fn()
  const { result } = renderHook(() => useQuantityStepper(2, 3, commit))

  act(() => result.current.change(1))
  act(() => result.current.change(1))
  expect(result.current.quantity).toBe(3)
  act(() => result.current.change(-1))
  act(() => result.current.change(-1))
  act(() => result.current.change(-1))
  expect(result.current.quantity).toBe(1)
})
