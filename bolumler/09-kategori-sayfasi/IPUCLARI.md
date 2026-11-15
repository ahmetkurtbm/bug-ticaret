# İpuçları · Bölüm 9 (FE-02)

Üç ipucu var, ağırlığı giderek artar. Önce kendiniz deneyin; takılınca sırayla açın.

## İpucu 1

Network sekmesinde tekrarlayan isteği seçip *Initiator* sütununa ve çağrı yığınına bakın; isteği hangi koddan tetiklendiğini gösterir. Sayfada sağ altta TanStack Query Devtools düğmesi var: `products` sorgusunun durumunun (fetching / idle) sürekli değiştiğini görebilirsiniz. Son commit'in hangi dosyalara dokunduğunu `git log -3 --stat` ile kontrol edin.

*EN:* In the Network tab, select a repeating request and look at the *Initiator* column and the call stack; they show which code triggered the request. There is a TanStack Query Devtools button at the bottom right of the page: you can see the status of the `products` query (fetching / idle) changing continuously. Check which files the latest commit touched with `git log -3 --stat`.

## İpucu 2

Kategori sayfası verisini TanStack Query ile (`useQuery`) çekiyor ve sorgunun anahtarı (`queryKey`) filtreler değişince değişiyor; kütüphane anahtar değişince veriyi zaten yeniden getirir. Bileşende ayrıca bir `useEffect` var mı? React'te bir bileşen state değişince yeniden çizilir (render); `useEffect` bağımlılık dizisindeki değerlerin önceki çizimdekiyle aynı olup olmadığına (`Object.is`) bakar. Bileşenin gövdesinde her çizimde yeni oluşturulan bir nesne ya da dizi var mı?

*EN:* The category page fetches its data with TanStack Query (`useQuery`), and the query key (`queryKey`) changes when filters change; the library already refetches when the key changes. Is there also a `useEffect` in the component? In React a component re-renders when state changes; `useEffect` compares the values in its dependency array with those of the previous render (`Object.is`). Is there an object or array in the component body that is newly created on every render?

## İpucu 3

`frontend/apps/storefront/src/pages/ProductListPage.tsx` içindeki `ProductListPage` bileşeninde `search` her çizimde yeni bir nesne olarak oluşuyor ve eklenen `useEffect` `[search]`'e bağımlı; efekt içinde `products.refetch()` çağrılıyor. `refetch` sorgunun durumunu değiştiriyor, bu yeniden çizim yaratıyor, yeni bir `search` nesnesi oluşuyor ve efekt tekrar çalışıyor. Döngünün kendisi efektten doğuyor; veriyi çekmek için zaten gerekmeyen bir efekt. Satırın yanındaki `eslint-disable` yorumuna da dikkat edin.

*EN:* In the `ProductListPage` component in `frontend/apps/storefront/src/pages/ProductListPage.tsx`, `search` is created as a new object on every render, and the added `useEffect` depends on `[search]`; inside the effect `products.refetch()` is called. `refetch` changes the query's state, which causes a re-render, a new `search` object is created and the effect runs again. The loop comes from the effect itself, which is not needed to fetch data anyway. Also note the `eslint-disable` comment next to that line.
