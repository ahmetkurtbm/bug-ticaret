import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { cartApi } from '../api/cart'
import type { Cart } from '../api/types'

export const cartQueryKey = ['cart'] as const

export function useCart() {
  return useQuery({ queryKey: cartQueryKey, queryFn: ({ signal }) => cartApi.get(signal), staleTime: 30_000 })
}

/** Sepeti değiştiren tüm işlemler sunucunun döndürdüğü güncel sepeti önbelleğe yazar. */
function useCartMutation<TArgs>(fn: (args: TArgs) => Promise<Cart>) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: fn,
    onSuccess: (cart) => queryClient.setQueryData(cartQueryKey, cart),
  })
}

export const useAddToCart = () => useCartMutation(({ variantId, quantity }: { variantId: number; quantity: number }) => cartApi.add(variantId, quantity))
export const useUpdateCartItem = () => useCartMutation(({ itemId, quantity }: { itemId: number; quantity: number }) => cartApi.update(itemId, quantity))
export const useRemoveCartItem = () => useCartMutation((itemId: number) => cartApi.remove(itemId))
export const useApplyCoupon = () => useCartMutation((code: string) => cartApi.applyCoupon(code))
export const useRemoveCoupon = () => useCartMutation(() => cartApi.removeCoupon())
