# Çözüm · Bölüm 2 (FE-01) · Koşullu hook çağrısı

## Kök neden

`ProductPage.tsx` içindeki `ProductPage` bileşeni, `useRecentlyViewed(product.data.id)` hook'unu erken `return`'lerden sonra çağırıyor:

```tsx
if (product.isPending) return <div ... />      // 1. çizim burada biter
if (product.isError) return <ErrorState ... />
useRecentlyViewed(product.data.id)             // yalnızca veri geldikten sonra çalışır
return <ProductView ... />
```

React hook'ları adlarıyla değil çağrılma sırasıyla eşleştirir. İlk çizimde (`isPending`) bileşen `useParams` ve `useQuery` hook'larını çağırıp çıkar. Veri gelince ikinci çizimde bir `useEffect` daha çağrılır. Sayı arttığı için React `Rendered more hooks than during the previous render.` hatası fırlatır. Uygulamada hata sınırı (error boundary) olmadığından React bütün ağacı söker ve ekran beyaz kalır.

Değişikliği yapan geliştirici şunu düşünmüş (commit `627329f`):

> Ürün sayfası: son bakılan ürünler kaydediliyor
> Ana sayfadaki "Son baktıkların" şeridi için ilk adım.

Hook'u `product.data.id` ile çağırmak istemiş. TypeScript `data`'nın tanımlı olduğunu ancak iki erken `return`'den sonra bilir; o yüzden çağrıyı oraya koymuş. Tip hatasından kurtulmak için hook kuralını çiğnemiş.

## Nasıl bulunur

1. **Belirti:** Beyaz ekran. Önce konsola bakın: `Rendered more hooks than during the previous render.`
2. **Araç: konsol.** Hata mesajının altında "The above error occurred in the `<ProductPage>` component" yazar. Bileşen adı elinizde.
3. **Araç: lint.** `cd frontend/apps/storefront && npx oxlint` (ya da `npm run lint -w storefront`). `.oxlintrc.json` içinde `react/rules-of-hooks` açık; hata dosya ve satırı gösterir: hook'un çağrıldığı yer, erken `return`'den sonra.
4. **Araç: git.** `git log -3 --stat` ve `git show HEAD` en son commit'in `ProductPage.tsx` ve yeni bir `useRecentlyViewed.ts` eklediğini gösterir. "Dün çalışıyordu" bilgisi, suçlunun son değişiklikte olduğunu söyler.
5. **Dosya:** `frontend/apps/storefront/src/pages/ProductPage.tsx`. `ProductPage` içinde `return`'lerin ve hook çağrısının sırasına bakın.
6. **Doğrulama:** İki çizimi karşılaştırın: yükleniyor çiziminde hook listesi `useParams` ve `useQuery` ile biter; veri çiziminde listeye `useRecentlyViewed` içindeki `useEffect` eklenir.

**Visual Studio:** Frontend için Visual Studio yerine tarayıcı araçlarını ve terminali kullanın (bkz. docs/IDE.md).

**VS Code:** "Müşteri sitesi (Chrome)" yapılandırmasıyla başlatın, Çalıştır ve Hata Ayıkla panelinde *Breakpoints* → "Uncaught Exceptions" kutusunu işaretleyin. React'in fırlattığı hatada durur, çağrı yığınında `ProductPage` görünür.

## Düzeltme

Hook'u, veri varken çizilen `ProductView` içine alın. Orada `product` zaten dolu, tip sorunu da kalmaz.

```diff
@@ ProductPage
   if (product.isPending) return <div className="h-96 animate-pulse rounded-xl bg-line/60" aria-busy="true" />
   if (product.isError) return <ErrorState error={product.error} onRetry={() => void product.refetch()} />
-  useRecentlyViewed(product.data.id)
   // key: farklı ürüne geçince seçili varyant ve görsel durumu sıfırlansın
   return <ProductView key={product.data.id} product={product.data} />
 }

 function ProductView({ product }: { product: ProductDetail }) {
+  useRecentlyViewed(product.id)
   const [imageIndex, setImageIndex] = useState(0)
```

## Neden böyle düzeltiyoruz

- **Hook'u `if` içinde bırakıp sonuna `else` koymak** sorunu çözmez; kural koşulsuz çağrıdır.
- **`ProductPage` en üstüne `useRecentlyViewed(product.data?.id)` koymak** da çalışır, ama hook'un `undefined` kabul etmesi, içinde ek bir `if` gerektirir ve gereksiz yere "veri yok" durumunu hook'a sızdırır. `ProductView` zaten yalnızca ürün varken var olur ve `key` ile ürün değişince yeniden kurulur; hook ihtiyacını tam karşılar.
- **Tip hatasını `product.data!` ile susturmak** hatayı gizler, çökmeyi çözmez. Yükleniyor çiziminde `data` gerçekten `undefined`'dır.
- **Lint kuralını `// eslint-disable` ile kapatmak** aynı hatayı bir sonraki geliştiriciye bırakır.

## Bir daha olmaması için

- **Lint:** `react/rules-of-hooks` zaten `error`. CI'da `npm run lint` adımı zorunlu olsun ve başarısız olursa birleştirme engellensin.
- **Hata sınırı:** Yönlendirme (route) düzeyinde bir `ErrorBoundary` ekleyin. Bir sayfadaki çizim hatası bütün siteyi beyaz bırakmamalı; kullanıcı menüyü kullanıp başka sayfaya geçebilmeli ve hata izlemeye gitmeli.
- **Test:** Bu bölümün testi gibi, bileşeni "yükleniyor → veri geldi" geçişiyle çizen bir test koyun. Hook sırası hatası yalnızca bu geçişte görünür.
- **Kod inceleme maddesi:** Bir bileşende `return`'den sonra `use...` görüyorsanız birleştirmeyin. "Tip uyuşmuyor diye yeri değiştirdim" dediyse hook'u veri varken çizilen alt bileşene taşıtın.

## İlgili

HATALAR.md · **FE-01 Koşullu hook çağrısı**

## English summary

**Root cause:** in `ProductPage.tsx`, `useRecentlyViewed(product.data.id)` was called after the early returns for the pending and error states. The loading render calls fewer hooks than the data render, so React throws "Rendered more hooks than during the previous render." With no error boundary, the whole tree is unmounted and the page goes white. The developer (commit "Ürün sayfası: son bakılan ürünler kaydediliyor") put the call there because TypeScript only knows `data` is defined after the early returns.

**How to find it:** console error and the component named in "The above error occurred in..." -> `npx oxlint` in `frontend/apps/storefront` (`react/rules-of-hooks`) -> `git log -3 --stat` -> `ProductPage.tsx`.

**Fix:** call the hook inside `ProductView`, which only renders when the product exists: `useRecentlyViewed(product.id)`.

**Prevent:** enforce lint in CI, add a route-level error boundary, test the loading-to-data transition, and reject any `use...` call after a `return` in review.
