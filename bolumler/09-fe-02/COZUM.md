# Çözüm · Bölüm 9 (FE-02) · Sonsuz render döngüsü

## Kök neden

`frontend/apps/storefront/src/pages/ProductListPage.tsx` içinde commit `90ea52a` şu efekti eklemiş:

```tsx
const search = { category: slug, q, sort, page, minPrice, maxPrice, pageSize: 24 }
const products = useQuery({ queryKey: ['products', search], queryFn: ..., placeholderData: keepPreviousData })

useEffect(() => {
  void products.refetch()
}, [search]) // eslint-disable-line react-hooks/exhaustive-deps
```

Döngü şöyle işler:

1. Bileşen çizilir. Gövdede `search` **yeni bir nesne** olarak oluşur (içeriği aynı olsa da bellekte yeni).
2. React efekti çalıştırmak için bağımlılıkları `Object.is` ile karşılaştırır. Yeni nesne eskiyle aynı değildir, efekt çalışır.
3. Efekt `products.refetch()` çağırır. Sorgunun durumu değişir (`isFetching` true olur, sonra false).
4. Durum değişimi bileşeni yeniden çizdirir. 1. adıma dönülür.

Sonuç: Network'te saniyede onlarca istek; `refetch` varsayılan olarak çalışan isteği iptal edip yenisini başlattığı için çoğu "(canceled)" görünür. Sayfa donar, API boşuna yüklenir.

Üstelik efekt gereksiz. TanStack Query sorgu anahtarını (`['products', search]`) nesne içeriğine göre karşılaştırır (yapısal olarak özetler). Sıralama, sayfa ya da filtre değişince anahtar değişir ve kütüphane yeni veriyi zaten kendiliğinden getirir.

Değişikliği yapan geliştirici şunu düşünmüş (commit `90ea52a`):

> Kategori sayfası: filtre değişince liste tazeleniyor

Koddaki yorumu: "Filtre ya da sıralama değişince liste hemen tazelensin (önbellekteki eski sonuç gösterilmesin)." Yani önbellekteki eski sonuç görünmesin istemiş. Bu istek zaten karşılanıyor: anahtar değişince yeni veri getirilir (ve `keepPreviousData` yeni veri gelene kadar eskisini soluk gösterir, bu bilinçli bir tercih). Satırın sonuna `eslint-disable-line react-hooks/exhaustive-deps` eklemiş: bağımlılık listesinin eksik ya da şüpheli olduğunu bilerek uyarıyı susturmuş. O uyarı bu sorunun habercisiydi.

## Nasıl bulunur

1. **Belirti:** Fan çalışıyor, sayfa kasıyor. Network'te aynı `products?...` isteği saniyede onlarca kez.
2. **Araç: Network → *Initiator*.** İstek `ProductListPage` içindeki sorgudan geliyor. TanStack Query Devtools'ta (sağ alt) `products` sorgusu sürekli `fetching` ↔ `idle` arasında gidip geliyor.
3. **Araç: React Developer Tools → Profiler.** Kayıt başlatıp "Record why each component rendered while profiling" seçeneğini açın. `ProductListPage` hiçbir prop değişmeden sürekli çiziliyor; sebep "Hooks changed" (sorgu durumu).
4. **Yöntem: efekt içine log.** `useEffect` içine `console.log('efekt çalıştı')` yazın; konsol dolar. Bağımlılığı sorgulamak için `search`'ü bir `useRef` ile saklayıp `Object.is(prev.current, search)` yazdırın: her çizimde `false`.
5. **Dosya:** `ProductListPage.tsx`; `git log -p -- frontend/apps/storefront/src/pages/ProductListPage.tsx` yeni eklenen `useEffect` ve `eslint-disable` yorumunu gösterir. "Dün çalışıyordu" bilgisi, son commit'i işaret eder.
6. **Doğrulama:** Efekti yorum satırı yapınca istek akışı durur.

**Visual Studio:** Frontend için tarayıcı araçlarını kullanın.

**VS Code:** "Müşteri sitesi (Chrome)" yapılandırmasıyla efektin içine kesme noktası koyun; sürekli durur, çağrı yığını çizimden geldiğini gösterir. Döngüyü kırmak için kesme noktasını bir kez durdurduktan sonra kaldırın.

## Düzeltme

```diff
 import { keepPreviousData, useQuery } from '@tanstack/react-query'
-import { useEffect } from 'react'
 import { Link, useParams, useSearchParams } from 'react-router'
@@
     placeholderData: keepPreviousData,
   })

-  // Filtre ya da sıralama değişince liste hemen tazelensin (önbellekteki eski sonuç gösterilmesin).
-  useEffect(() => {
-    void products.refetch()
-  }, [search]) // eslint-disable-line react-hooks/exhaustive-deps
-
   function update(changes: Record<string, string | undefined>) {
```

## Neden böyle düzeltiyoruz

- **`search`'ü `useMemo` ile sabitlemek** döngüyü keser ama efekt gereksiz kalır: anahtar değişince sorgu zaten bir kez çekiyor, efekt ikinci bir istek yapar (ve ilkini iptal eder). Çift istek ve karmaşık kod kalır.
- **Efekt bağımlılığını ilkel değerlere çevirmek** (`[slug, q, sort, page, minPrice, maxPrice]`) döngüyü keser, ama yine her filtre değişiminde fazladan bir `refetch` yapar. Çözüm "daha doğru bağımlılık" değil, "efekti hiç yazmamak".
- **`eslint-disable` bırakıp başka yerden kırmak** uyarıyı tekrar susturmak demektir. Uyarı doğruydu.
- **Verinin her zaman taze olmasını gerçekten istiyorsak** bunu sorgu seçeneğiyle söyleriz: `staleTime: 0` (zaten varsayılan) ya da `refetchOnMount: 'always'`. Efektle değil.

Veri çekmek için `useEffect` yazmak çoğu zaman gereksizdir: efekt, React dışındaki bir şeyle (DOM olayı, abonelik, tarayıcı API'si) eşitlenmek içindir. Sunucu verisini yönetmek TanStack Query'nin işidir.

## Bir daha olmaması için

- **Lint:** `frontend/apps/storefront/.oxlintrc.json` şu an yalnızca `react/rules-of-hooks` ve `react/only-export-components` kurallarını içeriyor. `react/exhaustive-deps` kuralını ekleyin ve `eslint-disable` / `oxlint-disable` kullanımını açıklama zorunlu hâle getirin. Susturulan uyarı sormadan geçmesin.
- **Test:** Bu bölümün testi gibi, sayfa açılınca `catalogApi.search` çağrı sayısını sınayın (en fazla birkaç). Sonsuz döngü bu testle hemen fark edilir.
- **Kod inceleme maddesi:** Bileşen gövdesinde oluşturulan nesne/dizi/fonksiyonu bağımlılık dizisine koyan efekt görürseniz durun. İçinde `refetch`/`setState` varsa ve bağımlılık o state'e dokunuyorsa döngü kaçınılmazdır. "Efekt gerçekten gerekiyor mu?" sorusunu sorun.
- **İzleme:** Tarayıcıda aynı adrese kısa sürede çok istek giderse (istemci tarafı hız sınırı ya da sunucuda IP başına istek sayısı uyarısı) erken fark edilir; bu hata API'ye de gereksiz yük bindirir.

## İlgili

HATALAR.md · **FE-02 Sonsuz render döngüsü (useEffect bağımlılığı)**

## English summary

**Root cause:** `ProductListPage.tsx` got a `useEffect(() => { products.refetch() }, [search])`. `search` is a new object on every render, so the effect runs after every render; `refetch()` changes the query state, which re-renders, which creates a new `search`, and so on: an endless loop of requests (many show as "(canceled)" because `refetch` cancels the in-flight request). The effect is also unnecessary, since TanStack Query refetches when the `queryKey` changes. The developer (commit "Kategori sayfası: filtre değişince liste tazeleniyor") wanted fresh data on filter change and silenced the `exhaustive-deps` warning with `eslint-disable`.

**How to find it:** repeating request in Network (Initiator) -> TanStack Query Devtools flipping fetching/idle -> React DevTools Profiler "why did this render" -> `git log -p` shows the new effect.

**Fix:** delete the `useEffect` (and its import). The query key already triggers the refetch.

**Prevent:** enable `react/exhaustive-deps`, require a reason for lint suppressions, test the number of `search` calls, and question every effect that fetches data in review.
