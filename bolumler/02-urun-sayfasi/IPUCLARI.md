# İpuçları · Bölüm 2 (FE-01)

Üç ipucu var, ağırlığı giderek artar. Önce kendiniz deneyin; takılınca sırayla açın.

## İpucu 1

Tarayıcı konsolundaki hata mesajının altındaki yığına (stack) bakın; React hatanın hangi bileşende olduğunu "The above error occurred in the ..." satırıyla söyler. Bir de depoda `cd frontend/apps/storefront && npx oxlint` çalıştırın: bu projenin lint ayarı hook kurallarını denetliyor ve dosya ile satırı doğrudan gösterebilir. `git log main..HEAD` ya da `git log -3 --stat` son değişikliğin hangi dosyalara dokunduğunu söyler.

*EN:* Read the stack under the error message in the browser console; React names the failing component in the line "The above error occurred in the ...". Also run `cd frontend/apps/storefront && npx oxlint`: this project's lint config checks the hooks rules and can point at the file and line directly. `git log -3 --stat` shows which files the latest change touched.

## İpucu 2

React, hook'ları (`useState`, `useEffect`, `useQuery`, kendi yazdığınız `use...` fonksiyonları) her çizimde (render) aynı sayıda ve aynı sırayla çağrılmasını bekler. Bu sayfa önce "yükleniyor" durumunda, sonra veri gelince bir kez daha çizilir. İki çizimde çağrılan hook sayısını karşılaştırın; bileşendeki `return` satırlarının hook çağrılarına göre nerede durduğuna bakın.

*EN:* React expects hooks (`useState`, `useEffect`, `useQuery`, your own `use...` functions) to be called in the same number and order on every render. This page renders once while loading and again when the data arrives. Compare how many hooks are called in the two renders, and look at where the `return` statements sit relative to the hook calls in the component.

## İpucu 3

`frontend/apps/storefront/src/pages/ProductPage.tsx` içindeki `ProductPage` bileşeninde `useRecentlyViewed(...)` çağrısı, `isPending` ve `isError` için yazılmış erken `return`'lerden sonra duruyor. Yükleniyor çiziminde bu hook hiç çağrılmıyor, veri gelince çağrılıyor; hook sayısı artıyor ve React çöküyor. Hook'un yeri yanlış; `useRecentlyViewed` fonksiyonunun kendisi doğru.

*EN:* In `frontend/apps/storefront/src/pages/ProductPage.tsx`, the `ProductPage` component calls `useRecentlyViewed(...)` after the early `return`s for `isPending` and `isError`. During the loading render this hook is never called; once data arrives it is, so the hook count grows and React crashes. The hook's position is wrong; the `useRecentlyViewed` function itself is fine.
