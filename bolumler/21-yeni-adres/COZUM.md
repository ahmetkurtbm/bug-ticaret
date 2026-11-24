> **Cevap anahtarı.** Bölümü kendiniz çözmeden okumayın; önce `ipucu` komutunu deneyin.

# Çözüm · Bölüm 21 (FE-16) · Yanlış sorgu anahtarı, tazelenmeyen liste

## Kök neden

`frontend/apps/storefront/src/pages/CheckoutPage.tsx` içinde adres listesi şu anahtarla okunuyor:

```tsx
const addressesKey = ['addresses'] as const
...
const addresses = useQuery({ queryKey: addressesKey, queryFn: ({ signal }) => addressApi.list(signal) })
```

Yeni adres kaydı başarılı olunca `AddressForm` ise başka bir anahtarı bayat işaretliyor:

```tsx
onSuccess: async (a) => {
  // Hesaba ait sorgular ['me', ...] altında; çıkışta tek seferde temizlenebilsin.
  await queryClient.invalidateQueries({ queryKey: ['me', 'addresses'] })
  onCreated(a)
},
```

TanStack Query anahtarları **önek** olarak eşler: `invalidateQueries({ queryKey: ['me'] })` `['me', 'addresses']`'i de kapsar, ama `['me', 'addresses']` anahtarı `['addresses']` sorgusuyla eşleşmez; dizinin ilk elemanı bile farklıdır. Hiçbir sorgu eşleşmediği için hiçbir şey bayat işaretlenmez, liste yeniden çekilmez ve ekran eski veriyi göstermeye devam eder. Sayfa yenilenince önbellek sıfırlandığı için sunucudaki gerçek liste gelir. Hata sessizdir: `invalidateQueries` eşleşme olmadığında hata vermez.

Kayıt isteği ise sorunsuzdur (`POST` `201 Created`); sunucu adresi gerçekten oluşturur. Bu yüzden müşteriler "kaydedilmedi" sanıp tekrar eklemiş ve yinelenen adresler oluşmuştur.

Değişikliği yapan geliştirici şunu düşünmüş (commit `abc1234`):

> Sorgu anahtarları hesap altında toplanıyor (hazırlık)
> Çıkışta tüm kullanıcı verisi tek `removeQueries(['me'])` ile silinsin.

Fikir iyi: kullanıcıya ait bütün sorguları tek önek altında toplamak çıkışta temizliği kolaylaştırır. Ama yarım kalmış: yalnızca bir `invalidateQueries` çağrısı yeni anahtara taşınmış, sorgunun kendisi eski anahtarda kalmış. Anahtar bir "dize"dir; derleyici iki tarafın aynı anahtarı kastettiğini denetleyemez.

## Nasıl bulunur

1. **Belirti:** Kayıttan sonra liste değişmiyor, yenileyince doğru. Bu, sunucuda veri doğru ama istemcinin elindeki kopya eski demektir.
2. **Araç: Network sekmesi.** "Adresi kaydet"ten sonra `POST /api/me/addresses` (ya da adres ucu) `201` döner; **ardından hiçbir `GET` gitmez**. Beklenen davranış, kaydın ardından listenin yeniden istenmesidir. Yenileyince `GET` gider ve adres gelir.
3. **Araç: React Query Devtools** (ya da kodda `queryClient.getQueryCache().getAll().map(q => q.queryKey)`). Önbellekte `['addresses']` anahtarı görünür; `invalidateQueries`'ın çağrıldığı anahtar `['me', 'addresses']`'tir. Aynı şeyi iki ayrı yerde yazılmış iki dizi olarak görürsünüz.
4. **Kod:** `CheckoutPage.tsx` içinde `addressesKey` tanımını ve `invalidateQueries` çağrısını arayın. `git log -p -- frontend/apps/storefront/src/pages/CheckoutPage.tsx` yarım kalan anahtar düzenlemesini gösterir.

## Düzeltme

En küçük değişiklik, form başarıyla bitince listeyi okuyan anahtarı kullanmaktır:

```diff
 function AddressForm({ onCreated, onCancel }: { onCreated: (a: Address) => void; onCancel?: () => void }) {
   ...
     onSuccess: async (a) => {
-      // Hesaba ait sorgular ['me', ...] altında; çıkışta tek seferde temizlenebilsin.
-      await queryClient.invalidateQueries({ queryKey: ['me', 'addresses'] })
+      await queryClient.invalidateQueries({ queryKey: addressesKey })
       onCreated(a)
     },
```

Daha sağlam hâli: anahtarları tek yerde, bir **anahtar fabrikasında** tanımlayın ve hem `useQuery` hem `invalidateQueries` oradan okusun:

```ts
// frontend/apps/storefront/src/api/queryKeys.ts
export const queryKeys = {
  addresses: () => ['addresses'] as const,
  orders: () => ['orders'] as const,
}

// CheckoutPage.tsx
const addresses = useQuery({ queryKey: queryKeys.addresses(), queryFn: ({ signal }) => addressApi.list(signal) })
...
await queryClient.invalidateQueries({ queryKey: queryKeys.addresses() })
```

Çıkışta temizlik isteniyorsa anahtarlar fabrikada **birlikte** `['me', 'addresses']` biçimine taşınır; böylece okuma ve tazeleme aynı anda, aynı yerde değişir.

## Neden böyle düzeltiyoruz

- **Sayfayı yeniden yüklemek** (`window.location.reload()`). Belirtiyi saklar ama formun durumunu, sepet seçimini ve kullanıcının yerini kaybettirir; kök nedene dokunmaz.
- **Yerel listeye elle eklemek.** `setQueryData(addressesKey, (old) => [...old, a])` kullanılabilir ve ek bir istek harcamaz. Ama sunucunun belirlediği değerleri (varsayılan adres bayrağı, sıralama) istemci tahmin etmek zorunda kalır. Sunucudaki gerçek listeyi yeniden çekmek (`invalidateQueries`) daha basit ve doğrudur.
- **`invalidateQueries()` ile her şeyi bayat işaretlemek.** Çalışır ama gereksiz isteklere yol açar (sepet, siparişler, vb. yeniden çekilir). Doğru anahtarı hedeflemek yeterli.
- **Sorguya `refetchOnMount` / kısa `staleTime` ayarı.** Sorunun tazelemeyi tetikleyen bölümünü değil, belirtisini örter.

## Bir daha olmaması için

- **Anahtar fabrikası:** Dize/dizi anahtarlarını dosyalara dağıtmayın; tek modülde işlev olarak tanımlayın. Yeniden adlandırma tek yerde yapılır ve TypeScript kullanım yerlerini gösterir.
- **Test:** Bu bölümün kontrolü gibi bir arayüz testi: `CheckoutPage`'i gerçek bir `QueryClient` ile çizin, adres API'sini sahte yanıtla (örneğin MSW) çalıştırın, formu doldurup kaydedin ve yeni adresin **sayfa yenilenmeden** listede göründüğünü doğrulayın. Anahtar uyuşmazlığı bu testi kırar.
- **Kod inceleme maddesi:** Sorgu anahtarının biçimini değiştiren bir PR'da o anahtarı kullanan **bütün** `useQuery`, `invalidateQueries`, `setQueryData` çağrılarının güncellendiğini kontrol edin ("anahtarı aratın" yeterli). "Hazırlık" ya da "yarım" etiketli değişiklikleri tek başına birleştirmeyin.
- **Hata ayıklama alışkanlığı:** Sunucu doğru, ekran eski diyorsa önce "kayıttan sonra yeni bir `GET` gitti mi?" diye bakın; gitmediyse sorun istemcinin tazeleme mantığındadır.

## İlgili

HATALAR.md · **FE-16 Önbellekte kalan eski veri**

## English summary

**Root cause:** the address list is read with the query key `['addresses']`, but after a successful save `AddressForm` calls `invalidateQueries({ queryKey: ['me', 'addresses'] })`. TanStack Query matches keys by prefix from the start, so `['me', 'addresses']` does not match `['addresses']`; nothing is marked stale, the list is not refetched, and the screen shows old data until the page is refreshed. The save itself works (`POST` returns 201), so customers added duplicates. The developer (commit "Sorgu anahtarları hesap altında toplanıyor (hazırlık)") started moving keys under `['me']` but changed only one call.

**How to find it:** Network shows a successful `POST` and no follow-up `GET`; the query cache (Devtools) holds `['addresses']`; the invalidate call uses a different key; `git log -p`.

**Fix:** invalidate the key the list uses (`addressesKey`), preferably by defining all query keys in one factory used by both `useQuery` and `invalidateQueries`.

**Prevent:** a query key factory, a UI test that saves an address and expects it in the list without a reload, and a review rule to check every use of a key whenever its shape changes.
